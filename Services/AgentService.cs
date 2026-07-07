using AgentOllamaPOC.Agents;
using AgentOllamaPOC.Execution;
using AgentOllamaPOC.Memory;
using AgentOllamaPOC.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;

namespace AgentOllamaPOC.Services;

public class AgentService
{
    private readonly RouterAgent _routerAgent;
    private readonly ConversationManager _conversationManager;
    private readonly MemoryService _memoryService;
    private readonly ILogger<AgentService> _logger;
    private readonly ConversationSummaryService _conversationSummaryService;
    private const int SummaryThreshold = 30;

    public AgentService( RouterAgent routerAgent, ConversationManager conversationManager, MemoryService memoryService, ILogger<AgentService> logger, ConversationSummaryService conversationSummaryService)
    {
        _routerAgent = routerAgent;
        _conversationManager = conversationManager;
        _memoryService = memoryService;
        _logger = logger;
        _conversationSummaryService = conversationSummaryService;
    }

    public async Task<string> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        try
        {

            if (string.IsNullOrWhiteSpace(question))
            {
                return "Please enter a valid question.";
            }


            var conversation = _conversationManager.Current;

            await _memoryService.AddUserMessageAsync(
               conversation.Id,
               question,
               cancellationToken);

            var context = new AgentContext
            {
                Conversation = conversation,
                Question = question
            };

            var answer = await _routerAgent.AskAsync<string>(context, cancellationToken);

            await _memoryService.AddAssistantMessageAsync(
                conversation.Id,
                answer.Output,
                cancellationToken);

            // Generate summary if needed
            var recentMessages = await _memoryService.GetMessageCountAsync(conversation.Id, cancellationToken: cancellationToken);

            if (recentMessages >= SummaryThreshold)
            {
                await _conversationSummaryService.SummarizeAsync(
                    context,
                    cancellationToken);
            }

            return answer.Output;

        }
        catch (Exception ex)
        {

            return
                $"Agent execution failed: {ex.Message}";

        }
    }

    public async IAsyncEnumerable<string> AskStreamingAsync(string question, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
             yield return "Please enter a valid question.";
             yield break;
        }

        var conversation = _conversationManager.Current;

        await _memoryService.AddUserMessageAsync(
               conversation.Id,
               question,
               cancellationToken);

        var context = new AgentContext
        {
            Conversation = conversation,
            Question = question
        };

        var builder = new StringBuilder();

        
        await foreach (var chunk in _routerAgent.AskStreamingAsync<string>(context, cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Text))
            {
                builder.Append(chunk.Text);
                yield return chunk.Text;
            }
        }

        await _memoryService.AddAssistantMessageAsync(
            conversation.Id,
            builder.ToString(),
            cancellationToken);

        // Generate summary if needed
        var recentMessages = await _memoryService.GetMessageCountAsync(conversation.Id, cancellationToken: cancellationToken);

        if (recentMessages >= SummaryThreshold)
        {
            await _conversationSummaryService.SummarizeAsync(context, cancellationToken);
        }

    }


    public async Task ClearConversationAsync(
        CancellationToken cancellationToken = default)
    {
        var conversation = _conversationManager.Current;

        await _memoryService.ClearAsync(
            conversation.Id,
            cancellationToken);

        _conversationManager.Create();
    }

}