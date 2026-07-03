
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

        await _db.ListRightPushAsync(GetKey(conversationId), json);

        await _db.KeyExpireAsync(GetKey(conversationId), SessionTtl);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(Guid conversationId, int limit = 10, CancellationToken cancellationToken = default)
    {
        var values = await _db.ListRangeAsync(GetKey(conversationId), -limit, -1);

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
}