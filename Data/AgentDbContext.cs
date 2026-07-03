using AgentOllamaPOC.Memory.Models;
using Microsoft.EntityFrameworkCore;

namespace AgentOllamaPOC.Data;
public class AgentDbContext : DbContext
{
    public AgentDbContext(DbContextOptions<AgentDbContext> options)
        : base(options)
    {
    }

    public DbSet<ConversationSummary> ConversationSummaries => Set<ConversationSummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ConversationSummary>()
        .HasKey(x => x.ConversationId);

        modelBuilder.Entity<ConversationSummary>()
            .Property(x => x.Summary)
            .IsRequired();

        modelBuilder.Entity<ConversationSummary>()
            .Property(x => x.LastProcessedMessageCount)
            .HasDefaultValue(0);
    }

}
