using AgentOllamaPOC.Rag;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace AgentOllamaPOC.Tools;

public class RagToolService
{

    private readonly RagService _rag;
    private readonly ILogger<RagToolService> _logger;

    public RagToolService(RagService rag, ILogger<RagToolService> logger)
    {
        _rag = rag;
        _logger = logger;
    }

    public async Task<string> SearchRepositoryKnowledgeAsync([Required] string query)
    {

        _logger.LogInformation("RAG TOOL EXECUTED");

        _logger.LogInformation(query);

        return await _rag.SearchAsync(query);

    }

}