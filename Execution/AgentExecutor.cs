using AgentOllamaPOC.Infrastructure;
using AgentOllamaPOC.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ClientModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace AgentOllamaPOC.Execution;

public sealed class AgentExecutor : IAgentExecutor
{
    private readonly IChatClient _chatClient;
    private readonly PromptBuilder _promptBuilder;
    private readonly ILogger<AgentExecutor> _logger;

    private readonly string _providerName;
    private readonly string _modelName;

    public AgentExecutor(
        IChatClient chatClient,
        PromptBuilder promptBuilder,
        ILogger<AgentExecutor> logger,
        IOptions<LlmProviderOptions> llmOptions)
    {
        _chatClient = chatClient;
        _promptBuilder = promptBuilder;
        _logger = logger;

        var options = llmOptions.Value;

        _providerName = options.Provider;
        _modelName = options.Model;
    }

    public async Task<ExecutionResult<T>> ExecuteAsync<T>(
        AgentContext context,
        string promptFile = "DefaultPrompt.txt",
        ExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IList<ChatMessage> messages;

            using (AgentTelemetry.StartActivity("prompt.build", "AgentExecutor"))
            {
                messages = await _promptBuilder.BuildAsync(
                    context,
                    promptFile,
                    cancellationToken);
            }

            var chatOptions = BuildChatOptions<T>(options);

            using var activity = AgentTelemetry.StartActivity("llm.request","AgentExecutor");

            activity?.SetTag("llm.provider", "configured");
            activity?.SetTag("llm.operation", "complete");
            activity?.SetTag("llm.response.format", "json");

            var stopwatch = Stopwatch.StartNew();
            var success = false;

            try
            {
                var response = await _chatClient.GetResponseAsync(messages, chatOptions, cancellationToken);

                AgentTelemetry.RecordTokenUsage(provider: _providerName, model: response.ModelId ?? _modelName, 
                    inputTokens: response.Usage?.InputTokenCount, outputTokens: response.Usage?.OutputTokenCount, totalTokens: response.Usage?.TotalTokenCount);

                success = true;

                return new ExecutionResult<T>
                {
                    Output = JsonResponseParser.Parse<T>(response.Text)
                };
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
            finally
            {
                stopwatch.Stop();

                AgentTelemetry.RecordLlmRequest(
                    "configured",
                    stopwatch.Elapsed.TotalMilliseconds,
                    success);
            }
        }
        catch (ClientResultException ex)
        {
            _logger.LogError(ex,"LLM returned {Status}",ex.Status);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Agent execution failed");

            throw;
        }
    }

    public async IAsyncEnumerable<StreamingChunk<T>> ExecuteStreamingAsync<T>(AgentContext context,string promptFile = "DefaultPrompt.txt",
        ExecutionOptions? options = null,[EnumeratorCancellation]CancellationToken cancellationToken = default)
    {
        IList<ChatMessage> messages;

        using (AgentTelemetry.StartActivity("prompt.build", "AgentExecutor"))
        {
            messages = await _promptBuilder.BuildAsync(context, promptFile, cancellationToken);
        }

        var chatOptions = BuildChatOptions<T>(options);

        var builder = new StringBuilder();

        using var activity = AgentTelemetry.StartActivity("llm.request","AgentExecutor");

        activity?.SetTag("llm.provider", "configured");
        activity?.SetTag("llm.operation", "stream");
        activity?.SetTag("llm.response.format", "json");

        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            UsageDetails? usage = null;
            string? modelId = null;

            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, chatOptions, cancellationToken))
            {
                if (update.ModelId is not null)
                    modelId = update.ModelId;

                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        case FunctionCallContent functionCall:
                            _logger.LogInformation(
        "Function call: {FunctionName}, CallId: {CallId}, RawRepresentation: {HasRawRepresentation}",
        functionCall.Name,
        functionCall.CallId,
        functionCall.RawRepresentation is not null);
                            break;

                        case FunctionResultContent functionResult:
                            _logger.LogInformation(
                                "Function result received: {CallId}",
                                functionResult.CallId);
                            break;

                        case UsageContent usageContent:
                            usage = usageContent.Details;
                            break;

                        case TextContent text when !string.IsNullOrEmpty(text.Text):
                            builder.Append(text.Text);

                            yield return new StreamingChunk<T>
                            {
                                Text = text.Text
                            };
                            break;
                    }
                }
            }

            success = true;

            // Record usage only after successful stream completion.
            if (usage is not null)
            {
                AgentTelemetry.RecordTokenUsage(
                    provider: _providerName,
                    model: modelId ?? _modelName,
                    inputTokens: usage.InputTokenCount,
                    outputTokens: usage.OutputTokenCount,
                    totalTokens: usage.TotalTokenCount);
            }

            yield return new StreamingChunk<T>
            {
                IsCompleted = true,
                FinalResult = new ExecutionResult<T>
                {
                    Output = JsonResponseParser.Parse<T>(builder.ToString())
                }
            };

        }
        finally
        {
            stopwatch.Stop();

            if (!success)
                activity?.SetStatus(ActivityStatusCode.Error, "Streaming execution did not complete");

            AgentTelemetry.RecordLlmRequest(
                "configured",
                stopwatch.Elapsed.TotalMilliseconds,
                success);
        }

    }

    private static ChatOptions BuildChatOptions<T>(ExecutionOptions? options)
    {
        if (options is null) return new ChatOptions { ResponseFormat = ChatResponseFormat.ForJsonSchema<T>() };

        return new ChatOptions
        {
            Tools = options.Tools?.ToList() ?? [],
            Temperature = options.Temperature,
            MaxOutputTokens = options.MaxOutputTokens,
            ResponseFormat = ChatResponseFormat.ForJsonSchema<T>()
        };
    }
}