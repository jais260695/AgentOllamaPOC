using Microsoft.Extensions.AI;
using Polly;


namespace AgentOllamaPOC.Services;


public class ResilientChatClient : IChatClient
{

    private readonly IChatClient _inner;


    private readonly AsyncPolicy _retryPolicy;



    public ResilientChatClient(
        IChatClient inner)
    {

        _inner = inner;


        _retryPolicy =
            Policy
            .Handle<Exception>(ex =>
            {
                return ex.Message.Contains("429")
                    || ex.Message.Contains("500")
                    || ex.Message.Contains("503");
            })
            .WaitAndRetryAsync(
                retryCount: 5,

                sleepDurationProvider:
                    retry =>
                        TimeSpan.FromSeconds(
                            Math.Pow(2, retry)
                        ),
                onRetry:
                    (exception, delay, retry, context) =>
                    {

                        Console.WriteLine(
                            $"LLM retry {retry} after {delay.TotalSeconds}s"
                        );

                        Console.WriteLine(
                            exception.Message
                        );

                    });

    }



    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {

        return await _retryPolicy
            .ExecuteAsync(
                async () =>
                {
                    try
                    {
                        return await _inner
                            .GetResponseAsync(
                                messages,
                                options,
                                cancellationToken
                            );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"LLM request failed: {ex.Message}"
                        );
                        throw;
                    }

                });

    }




    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {


        var stream =
            await _retryPolicy
                .ExecuteAsync(
                    async () =>
                    {

                        return _inner
                            .GetStreamingResponseAsync(
                                messages,
                                options,
                                cancellationToken
                            );

                    });


        await foreach (var item in stream)
        {

            yield return item;

        }

    }




    public object? GetService(
        Type serviceType,
        object? serviceKey = null)
    {

        return _inner.GetService(
            serviceType,
            serviceKey
        );

    }




    public void Dispose()
    {

        if (_inner is IDisposable disposable)
        {

            disposable.Dispose();

        }

    }

}