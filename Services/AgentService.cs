using AgentOllamaPOC.Agents;
using AgentOllamaPOC.Execution;
using AgentOllamaPOC.Guardrails.Abstractions;
using AgentOllamaPOC.Infrastructure;
using AgentOllamaPOC.Memory;
using AgentOllamaPOC.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using AgentOllamaPOC.Guardrails.Abstractions;
using AgentOllamaPOC.Guardrails.Models;

namespace AgentOllamaPOC.Services;

public class AgentService
{
    private readonly RouterAgent _routerAgent;
    private readonly ConversationManager _conversationManager;
    private readonly MemoryService _memoryService;
    private readonly ILogger<AgentService> _logger;
    private readonly ConversationSummaryService _conversationSummaryService;
    private readonly SemanticMemoryService _semanticMemoryService;
    private readonly MemoryExtractionService _memoryExtractionService;
    private readonly IGuardrailPipeline _guardrailPipeline;
    private const int SummaryThreshold = 30;

    public AgentService( RouterAgent routerAgent, ConversationManager conversationManager, MemoryService memoryService, ILogger<AgentService> logger, 
        ConversationSummaryService conversationSummaryService, SemanticMemoryService semanticMemoryService, MemoryExtractionService memoryExtractionService,
        IGuardrailPipeline guardrailPipeline)
    {
        _routerAgent = routerAgent;
        _conversationManager = conversationManager;
        _memoryService = memoryService;
        _logger = logger;
        _conversationSummaryService = conversationSummaryService;
        _semanticMemoryService = semanticMemoryService;
        _memoryExtractionService = memoryExtractionService;
        _guardrailPipeline = guardrailPipeline;
    }

    public async Task<string> AskAsync(string question, CancellationToken cancellationToken = default)
    {

        const string agentName = "AgentService";

        var stopwatch = Stopwatch.StartNew();
        var success = false;

        using var activity = AgentTelemetry.StartActivity("agent.request", agentName);

        try
        {
            var guardrailResult = await _guardrailPipeline.ExecuteAsync(
                                            new GuardrailContext
                                            {
                                                Stage = GuardrailStage.Input,
                                                Content = question
                                            },
                                            cancellationToken
                                        );

            if (guardrailResult.Decision != GuardrailDecision.Allow)
            {
                return guardrailResult.UserMessage ?? "Your request couldn't be processed.";
            }

            var conversation = _conversationManager.Current;

            activity?.SetTag("conversation.id", conversation.Id.ToString());

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
                using var summaryActivity = AgentTelemetry.StartActivity("memory.summarize");
                await _conversationSummaryService.SummarizeAsync(
                    context,
                    cancellationToken);
            }


            using (AgentTelemetry.StartActivity("memory.extract"))
            {
                var facts = await _memoryExtractionService.ExtractAsync(context, cancellationToken);

                await _semanticMemoryService.StoreAsync(
                    conversation.Id, facts, cancellationToken);

                _logger.LogInformation("Extracted {FactCount} semantic memories", facts.Count);
            }

            success = true;
            return answer.Output;

        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Request cancelled");
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Agent request failed");

            return $"Agent execution failed: {ex.Message}";
        }
        finally
        {
            stopwatch.Stop();

            AgentTelemetry.RecordAgentExecution(
                agentName,
                stopwatch.Elapsed.TotalMilliseconds,
                success);
        }
    }

    public async IAsyncEnumerable<string> AskStreamingAsync(string question, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {

        const string agentName = "AgentService";

        var stopwatch = Stopwatch.StartNew();
        var success = false;

        using var activity = AgentTelemetry.StartActivity("agent.request.stream", agentName);

        try
        {
            var guardrailResult = await _guardrailPipeline.ExecuteAsync(
                                                new GuardrailContext
                                                {
                                                    Stage = GuardrailStage.Input,
                                                    Content = question
                                                },
                                                cancellationToken
                                          );

            if (guardrailResult.Decision != GuardrailDecision.Allow)
            {
                yield return guardrailResult.UserMessage ?? "Your request couldn't be processed.";
                yield break;
            }

            var conversation = _conversationManager.Current;

            activity?.SetTag("conversation.id", conversation.Id.ToString());
            activity?.SetTag("agent.streaming", true);

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


            await foreach (var chunk in _routerAgent.AskStreamingAsync<string>(context, cancellationToken).WithCancellation(cancellationToken))
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
                using var summaryActivity = AgentTelemetry.StartActivity("memory.summarize");
                await _conversationSummaryService.SummarizeAsync(context, cancellationToken);
            }

            using (AgentTelemetry.StartActivity("memory.extract"))
            {

                var facts = await _memoryExtractionService.ExtractAsync(context, cancellationToken);

                await _semanticMemoryService.StoreAsync(conversation.Id, facts, cancellationToken);

                _logger.LogInformation("Extracted {FactCount} semantic memories", facts.Count);
            }

            success = true;

        }
        finally
        {
            stopwatch.Stop();

            if (!success && cancellationToken.IsCancellationRequested)
                activity?.SetStatus(ActivityStatusCode.Error, "Request cancelled");

            AgentTelemetry.RecordAgentExecution(
                agentName,
                stopwatch.Elapsed.TotalMilliseconds,
                success);
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