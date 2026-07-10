using AgentOllamaPOC.Agents;
using AgentOllamaPOC.Data;
using AgentOllamaPOC.Data.Repositories;
using AgentOllamaPOC.Execution;
using AgentOllamaPOC.Infrastructure;
using AgentOllamaPOC.Mcp;
using AgentOllamaPOC.Memory;
using AgentOllamaPOC.Memory.Interfaces;
using AgentOllamaPOC.Memory.Redis;
using AgentOllamaPOC.Rag;
using AgentOllamaPOC.Services;
using AgentOllamaPOC.Tools;
using AgentOllamaPOC.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Qdrant.Client;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

//Memory Servcies

builder.Services.AddDbContext<AgentDbContext>(options =>
{
    options.UseNpgsql("Host=localhost;Port=5433;Database=agent-db;Username=agent;Password=agent");
});

builder.Services.AddScoped<IConversationSummaryRepository, ConversationSummaryRepository>();

builder.Services.AddSingleton(ConnectionMultiplexer.Connect("localhost:6379"));

builder.Services.AddSingleton<IMemoryStore, RedisMemoryStore>();

builder.Services.AddSingleton<MemoryService>();

builder.Services.AddScoped<PromptBuilder>();

builder.Services.AddSingleton<ConversationManager>();

builder.Services.AddSingleton<PromptService>();

builder.Services.AddScoped<ConversationSummaryService>();

builder.Services.AddSingleton<MemoryExtractionService>();

builder.Services.AddSingleton<ISemanticMemoryRepository, SemanticMemoryRepository>();

builder.Services.AddSingleton<SemanticMemoryService>();

builder.Services.AddSingleton(_ =>
{
    return new QdrantClient(
        "localhost",
        6334);
});

//Tools
builder.Services.AddScoped<IAgentExecutor, AgentExecutor>();
builder.Services.AddSingleton<RagToolService>();
builder.Services.AddSingleton<McpToolAdapter>();

// MCP Client
builder.Services.AddSingleton(
    sp =>
    {
        var client = new GithubMcpClient();
        return client.ConnectAsync().GetAwaiter().GetResult();
    }
);

// chat client

builder.Services.AddSingleton<IChatClient>(
    sp =>
    {
        var factory = new ChatClientFactory();
        var client = factory.Create();
        return new ResilientChatClient(
       client
   );
    }
);

//builder.Services.AddSingleton<IChatClient>(sp =>
//{

//    var factory =
//        new GeminiChatClientFactory();


//    var client = factory.Create();
//    return new ResilientChatClient(
//       client
//   );

//});

// Agents
builder.Services.AddScoped<GithubAgent>();
builder.Services.AddScoped<RagAgent>();

builder.Services.AddScoped<RouterAgent>();

builder.Services.AddScoped<AgentService>();


// RAG
builder.Services.AddSingleton<EmbeddingService>();

builder.Services.AddSingleton<RAGQdrantService>();

builder.Services.AddSingleton<RagService>();

builder.Services.AddSingleton<GithubRepositoryIndexer>();

// Background worker
builder.Services.AddSingleton<RepositoryIndexWorker>();

builder.Services.AddHostedService(
    sp => sp.GetRequiredService<RepositoryIndexWorker>()
);

var app =builder.Build();

// start background worker
await app.StartAsync();

await app.Services.GetRequiredService<ISemanticMemoryRepository>().InitializeAsync();
await app.Services.GetRequiredService<RAGQdrantService>().InitializeAsync();

var indexer = app.Services.GetRequiredService<RepositoryIndexWorker>();

await indexer.Completion;

// start chat loop

using var scope = app.Services.CreateScope();
var chat = scope.ServiceProvider.GetRequiredService<AgentService>();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// One conversation = one session
var conversationManager =
    app.Services.GetRequiredService<ConversationManager>();

var conversation = conversationManager.Create();

Console.WriteLine($"Conversation: {conversation.Id}");
Console.WriteLine("Type 'exit' to quit.");

//conversation.Id = new Guid("d4f68827-d95a-475d-8799-2f566440cf1d");

try
{
    while (true)
    {

        Console.Write("User: ");

        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
            continue;

        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            break;

        //var answer = await chat.AskAsync(input!, cts.Token);

        //Console.WriteLine($"Assistant: {answer}");

        await foreach (var chunk in chat.AskStreamingAsync(input,cts.Token))
        {
            Console.Write(chunk);
        }

        Console.WriteLine();
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("\nOperation cancelled.");
}

await app.StopAsync();