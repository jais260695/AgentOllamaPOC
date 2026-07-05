
using AgentOllamaPOC.Memory.Interfaces;
using AgentOllamaPOC.Memory.Models;
using Microsoft.Extensions.AI;
using StackExchange.Redis;
using System.Text.Json;

namespace AgentOllamaPOC.Memory.Redis;

public sealed class RedisMemoryStore : IMemoryStore
{
    private readonly IDatabase _db;

    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(12);

    public RedisMemoryStore(ConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    private static string GetKey(Guid conversationId) => $"conversation:{conversationId}";

    public async Task AppendAsync(Guid conversationId , ChatMessage message, CancellationToken cancellationToken = default)
    {
        var stored = new StoredMessage
        {
            Role = message.Role.Value,
            Content = message.Text ?? string.Empty,
            TimestampUtc = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(stored);

        var tran = _db.CreateTransaction();

        Task<long> pushTask = tran.ListRightPushAsync(GetKey(conversationId), json);

        Task<bool> expireTask = tran.KeyExpireAsync(GetKey(conversationId), SessionTtl);

        bool transactionCommitted = await tran.ExecuteAsync();

        if (transactionCommitted)
        {
            // 3. It is now safe to read the results of the individual tasks
            long totalLengthAfterPush = await pushTask;
            bool expirySuccess = await expireTask;

            Console.WriteLine($"Message pushed. Total items: {totalLengthAfterPush}");
            Console.WriteLine($"TTL updated successfully: {expirySuccess}");
        }
        else
        {
            // The transaction failed (e.g., optimistic locking failed via redis 'WATCH')
            Console.WriteLine("Transaction aborted by Redis server.");
        }
    }

    public async Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(Guid conversationId, int start = -10, int stop = -1, CancellationToken cancellationToken = default)
    {
        var values = await _db.ListRangeAsync(GetKey(conversationId), start, stop);

        return values.Select(v =>
                                {
                                    var stored = JsonSerializer.Deserialize<StoredMessage>(v.ToString()) ?? new StoredMessage();

                                    return new ChatMessage(
                                        new ChatRole(stored.Role),
                                        stored.Content);
                                }
                             ).ToList();
    }

    public async Task ClearAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        await _db.KeyDeleteAsync(GetKey(conversationId));
    }

    public async Task<int> TrimHistoryAsync(Guid conversationId, int keepLastMessages, CancellationToken cancellationToken = default)
    {
        var key = GetKey(conversationId);

        var length = await _db.ListLengthAsync(key);

        if (length <= keepLastMessages)
            return 0;

        var trimmed = (int)(length - keepLastMessages);

        await _db.ListTrimAsync( key, length - keepLastMessages,length - 1);

        return trimmed;
    }

    public async Task<int> GetMessageCountAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var key = GetKey(conversationId);

        return (int)await _db.ListLengthAsync(key);
    }
}