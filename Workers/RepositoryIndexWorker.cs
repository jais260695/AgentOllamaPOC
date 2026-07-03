using AgentOllamaPOC.Rag;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgentOllamaPOC.Workers;

public class RepositoryIndexWorker : BackgroundService
{
    public Task Completion => _completion.Task;

    private readonly TaskCompletionSource<bool> _completion = new();

    private readonly GithubRepositoryIndexer _indexer;

    private readonly RagService _ragService;

    private readonly ILogger<RepositoryIndexWorker> _logger;

    public RepositoryIndexWorker( GithubRepositoryIndexer indexer, RagService ragService, ILogger<RepositoryIndexWorker> logger)
    {
        _ragService = ragService;
        _indexer = indexer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _completion.SetResult(true);
        return;

        _logger.LogInformation("Repository Index Worker started");

        try
        {

            // Create Qdrant collection first

            await _ragService.InitializeAsync();

            _logger.LogInformation("RAG initialized");


            await _indexer.IndexRepositoryAsync(
                        owner: "jais260695",
                        repo: "CodeReviewAgent",
                        branch: "main",
                        stoppingToken
                  );


            _logger.LogInformation("Repository indexing completed");

        }
        catch (Exception ex)
        {

            _logger.LogError(ex,"Repository indexing failed");

        }

        _completion.SetResult(true);
    }

}