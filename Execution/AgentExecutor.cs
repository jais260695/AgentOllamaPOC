using AgentOllamaPOC.Infrastructure;
using AgentOllamaPOC.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace AgentOllamaPOC.Execution;

public sealed class AgentExecutor : IAgentExecutor
{
    private readonly IChatClient _chatClient;
    private readonly PromptBuilder _promptBuilder;
    private readonly ILogger<AgentExecutor> _logger;

    public AgentExecutor(
        IChatClient chatClient,
        PromptBuilder promptBuilder,
        ILogger<AgentExecutor> logger)
    {
        _chatClient = chatClient;
        _promptBuilder = promptBuilder;
        _logger = logger;
    }

    public async Task<ExecutionResult<T>> ExecuteAsync<T>(
        AgentContext context,
        string promptFile = "DefaultPrompt.txt",
        ExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await _promptBuilder.BuildAsync(
                                        context,
                                        promptFile,
                                        cancellationToken
                                  );

            var chatOptions = BuildChatOptions<T>(options);

            var response = await _chatClient.GetResponseAsync(
                                        messages,
                                        chatOptions,
                                        cancellationToken
                                  );

            return new ExecutionResult<T>
            {
                Output = JsonResponseParser.Parse<T>(response.Text)
            };
        }
        catch (ClientResultException ex)
        {
            _logger.LogError(
                ex,
                "LLM returned {Status}",
                ex.Status);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Agent execution failed");

            throw;
        }
    }

    public async IAsyncEnumerable<StreamingChunk<T>> ExecuteStreamingAsync<T>(
        AgentContext context,
        string promptFile = "DefaultPrompt.txt",
        ExecutionOptions? options = null,
        [EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        var messages =
            await _promptBuilder.BuildAsync(
                        context,
                        promptFile,
                        cancellationToken
                  );

        var chatOptions = BuildChatOptions<T>(options);

        var builder = new StringBuilder();

        await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, chatOptions,cancellationToken))
        {
            foreach (var content in update.Contents)
            {
                if (content is TextContent text && !string.IsNullOrEmpty(text.Text))
                {
                    builder.Append(text.Text);

                    yield return new StreamingChunk<T>
                    {
                        Text = text.Text
                    };
                }
            }
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