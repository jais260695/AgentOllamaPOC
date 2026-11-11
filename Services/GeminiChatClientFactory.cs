using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace AgentOllamaPOC.Services;


public class GeminiChatClientFactory
{

    public IChatClient Create()
    {

        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");


        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("GOOGLE_API_KEY missing");
        }


        var client =
            new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions
                {
                    Endpoint =
                    new Uri(
                    "https://generativelanguage.googleapis.com/v1beta/openai/"
                    )
                });


        var model = Environment.GetEnvironmentVariable("GOOGLE_MODEL")
            ?? "gemini-3.5-flash-lite";

        var chatClient = client
            .GetChatClient(model)
            .AsIChatClient();

        var signatureAwareClient =
            new GeminiThoughtSignatureChatClient(chatClient);

        return new ChatClientBuilder(signatureAwareClient)
            .UseFunctionInvocation()
            .Build();

    }



    private sealed class GeminiThoughtSignatureChatClient
        : DelegatingChatClient
    {
        private const string SignatureKey = "gemini.thought_signature";

        public GeminiThoughtSignatureChatClient(IChatClient innerClient)
            : base(innerClient)
        {
        }

        public override async IAsyncEnumerable<ChatResponseUpdate>
            GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages,
                ChatOptions? options = null,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Restore signatures on previous assistant tool calls
            // before sending the next request to Gemini.
            var requestMessages = PrepareOutgoingMessages(messages);

            var callIdsByIndex = new Dictionary<int, string>();
            var signaturesByIndex = new Dictionary<int, string>();
            var signaturesByCallId = new Dictionary<string, string>(
                StringComparer.Ordinal);

            await foreach (var update in base.GetStreamingResponseAsync(
                requestMessages, options, cancellationToken))
            {
                // The OpenAI adapter preserves the underlying streaming
                // update here, even though it doesn't preserve it on
                // the aggregated FunctionCallContent.
                if (update.RawRepresentation is
                    OpenAI.Chat.StreamingChatCompletionUpdate rawUpdate)
                {
                    foreach (var toolUpdate in rawUpdate.ToolCallUpdates)
                    {
                        var index = toolUpdate.Index;

                        if (!string.IsNullOrWhiteSpace(toolUpdate.ToolCallId))
                        {
                            callIdsByIndex[index] = toolUpdate.ToolCallId;
                        }

                        // Gemini's thought signature is an additional
                        // field in the OpenAI-compatible streaming payload.
#pragma warning disable SCME0001
                        var signaturePath = Encoding.UTF8.GetBytes(
                            $"$.choices[0].delta.tool_calls[{index}].extra_content.google.thought_signature");

                        if (rawUpdate.Patch.TryGetValue(
                            signaturePath, out string? signature)
                            && !string.IsNullOrWhiteSpace(signature))
                        {
                            signaturesByIndex[index] = signature;
                        }
#pragma warning restore SCME0001

                        if (callIdsByIndex.TryGetValue(index, out var callId)
                            && signaturesByIndex.TryGetValue(index, out var thoughtSignature))
                        {
                            signaturesByCallId[callId] = thoughtSignature;
                        }
                    }
                }

                // The adapter emits aggregated FunctionCallContent
                // after receiving all streaming chunks.
                foreach (var functionCall in
                    update.Contents.OfType<FunctionCallContent>())
                {
                    if (signaturesByCallId.TryGetValue(
                        functionCall.CallId, out var thoughtSignature))
                    {
                        (functionCall.AdditionalProperties ??= [])
                            [SignatureKey] = thoughtSignature;
                    }
                }

                yield return update;
            }
        }

        private static List<ChatMessage> PrepareOutgoingMessages(
            IEnumerable<ChatMessage> messages)
        {
            var result = messages.ToList();

            foreach (var message in result)
            {
                if (message.Role != ChatRole.Assistant)
                    continue;

                var functionCalls = message.Contents
                    .OfType<FunctionCallContent>()
                    .ToList();

                if (functionCalls.Count == 0)
                    continue;

                var signatures = new List<string?>();

                foreach (var functionCall in functionCalls)
                {
                    if (functionCall.AdditionalProperties?.TryGetValue(
                        SignatureKey, out var value) == true
                        && value is string signature)
                    {
                        signatures.Add(signature);
                    }
                    else
                    {
                        signatures.Add(null);
                    }
                }

                // Gemini 3 requires the original signature.
                // For parallel calls, Google may provide it only
                // on the first function-call part.
                if (signatures.All(string.IsNullOrWhiteSpace))
                {
                    throw new InvalidOperationException(
                        "Gemini function call is missing its thought signature.");
                }

                var toolCalls = new List<OpenAI.Chat.ChatToolCall>();

                foreach (var functionCall in functionCalls)
                {
                    var arguments = JsonSerializer.SerializeToUtf8Bytes(
                        functionCall.Arguments);

                    var toolCall =
                        OpenAI.Chat.ChatToolCall.CreateFunctionToolCall(
                            functionCall.CallId,
                            functionCall.Name,
                            BinaryData.FromBytes(arguments));

                    if (functionCall.AdditionalProperties?.TryGetValue(
                            SignatureKey, out var value) == true
                        && value is string signature)
                    {
                        var path = Encoding.UTF8.GetBytes(
                            "$.extra_content.google.thought_signature");

#pragma warning disable SCME0001
                        toolCall.Patch.Set(path, signature);
#pragma warning restore SCME0001
                    }

                    toolCalls.Add(toolCall);
                }

                var rawAssistantMessage =
                    new OpenAI.Chat.AssistantChatMessage(toolCalls);

                foreach (var text in message.Contents.OfType<TextContent>())
                {
                    if (!string.IsNullOrEmpty(text.Text))
                    {
                        rawAssistantMessage.Content.Add(
                            OpenAI.Chat.ChatMessageContentPart.CreateTextPart(
                                text.Text));
                    }
                }


                // OpenAIChatClient recognizes this raw representation
                // and sends it without rebuilding the assistant message.
                message.RawRepresentation = rawAssistantMessage;
            }

            return result;
        }
    }


}

