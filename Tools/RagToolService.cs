using AgentOllamaPOC.Models;
using AgentOllamaPOC.Rag;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text;

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

        //return await BuildContext(result);

    }

    public async ValueTask<string> BuildContext(List<RagSearchResult> results)
    {
        var sb = new StringBuilder();

        var r = results.OrderByDescending(r=>r.Score).FirstOrDefault();
        
            //sb.AppendLine($"FILE: {r.FileName}");
            //sb.AppendLine($"SCORE: {r.Score:0.00}");
            //sb.AppendLine($"TYPE: {r.Type}");

            //sb.AppendLine(new string('-', 60));
        
        _logger.LogInformation(sb.ToString());
        return await ValueTask.FromResult(sb.ToString());
    }

}