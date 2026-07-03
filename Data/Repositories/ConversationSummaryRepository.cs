using AgentOllamaPOC.Memory.Models;
using Microsoft.EntityFrameworkCore;

namespace AgentOllamaPOC.Data.Repositories;

public class ConversationSummaryRepository : IConversationSummaryRepository
{
    private readonly AgentDbContext _db;

    public ConversationSummaryRepository(AgentDbContext db)
    {
        _db = db;
    }

    public async Task<ConversationSummary?> GetAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await _db.ConversationSummaries.FirstOrDefaultAsync(x => x.ConversationId == conversationId, cancellationToken);
    }

    public async Task SaveAsync(ConversationSummary summary, CancellationToken cancellationToken = default)
    {
        var existing =  await _db.ConversationSummaries.FirstOrDefaultAsync(x => x.ConversationId == summary.ConversationId,cancellationToken);

        if (existing is null)
        {
            summary.CreatedAtUtc = DateTime.UtcNow;
            summary.UpdatedAtUtc = DateTime.UtcNow;

            _db.ConversationSummaries.Add(summary);
        }
        else
        {
            existing.Summary = summary.Summary;
            existing.LastProcessedMessageCount = summary.LastProcessedMessageCount;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}