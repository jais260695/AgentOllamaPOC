using AgentOllamaPOC.Memory;
using AgentOllamaPOC.Models;
using Microsoft.Extensions.AI;
using System.Diagnostics;

namespace AgentOllamaPOC.Infrastructure;

public sealed class PromptBuilder
{
    private readonly MemoryService _memoryService;

    private readonly PromptService _promptService;

    private readonly ConversationSummaryService _summaryService;

    private readonly SemanticMemoryService _semanticMemoryService;

    public PromptBuilder(MemoryService memoryService, PromptService promptService, ConversationSummaryService summaryService,
        SemanticMemoryService semanticMemoryService)
    {
        _memoryService = memoryService;
        _promptService = promptService;
        _summaryService = summaryService;
        _semanticMemoryService = semanticMemoryService;
    }

    public async Task<List<ChatMessage>> BuildAsync(AgentContext context, string promptFile = "DefaultPrompt.txt", CancellationToken cancellationToken = default, bool isHistoryRequired = true)
    {
        using var activity = AgentTelemetry.StartActivity("prompt.build");
        try
        {

            var messages = new List<ChatMessage>();

            using (AgentTelemetry.StartActivity("prompt.template.load"))
            {
                var prompt = _promptService.GetPrompt(promptFile);
                messages.Add(new ChatMessage(ChatRole.System, prompt));
            }

            if (context.IncludeSemanticMemory)
            {
                using var semanticActivity = AgentTelemetry.StartActivity("prompt.semantic_memory.retrieve");

                try
                {
                    var memories = await _semanticMemoryService.SearchAsync(context.Conversation.Id, context.Question, top: 5, cancellationToken);

                    foreach (var fact in memories)
                    {
                        messages.Add(new ChatMessage(ChatRole.System, $"Semantic Fact: {fact}"));
                    }

                    semanticActivity?.SetTag("memory.semantic.result_ocunt", memories.Count);
                }
                catch (Exception)
                {
                    semanticActivity?.SetStatus(ActivityStatusCode.Error, "Semantic memory retrieval failed");
                    throw;
                }
            }

            if (context.IncludeConversationSummary)
            {
                using var summaryActivity = AgentTelemetry.StartActivity("prompt.conversation_summary.retrieve");
                try
                {
                    var summary = await _summaryService.GetSummaryAsync(context.Conversation.Id, cancellationToken);

                    if (summary != null)
                    {
                        messages.Add(new ChatMessage(ChatRole.System, $"Conversation Summary:\n{summary.Summary}"));
                    }

                    summaryActivity?.SetTag("memory.summary.found", summary is not null);
                }
                catch (Exception)
                {
                    summaryActivity?.SetStatus(
                        ActivityStatusCode.Error, "Conversation summary retrieval failed");
                    throw;
                }
            }


            if (context.IncludeHistory)
            {
                using var historyActivity = AgentTelemetry.StartActivity("prompt.conversation_history.retrieve");

                try
                {
                    var history = await _memoryService.GetHistoryAsync(conversationId: context.Conversation.Id, cancellationToken: cancellationToken);

                    messages.AddRange(history);

                    historyActivity?.SetTag("memory.history.message_count", history.Count);
                }
                catch (Exception)
                {
                    historyActivity?.SetStatus(ActivityStatusCode.Error, "Conversation history retrieval failed");
                    throw;
                }
            }


            if (context.IncludeCurrentQuestion)
            {
                messages.Add(new ChatMessage(ChatRole.User, context.Question));
            }

            activity?.SetTag("prompt.message_count", messages.Count);

            return messages;

        }
        catch (Exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Prompt building failed");
            throw;
        }
    }
}