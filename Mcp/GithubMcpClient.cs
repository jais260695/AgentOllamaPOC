using ModelContextProtocol.Client;


namespace AgentOllamaPOC.Mcp;


public class GithubMcpClient
{

    public async Task<McpClient> ConnectAsync()
    {
        // Add validation to provide a clearer error message:
        var token = Environment.GetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException(
                "GITHUB_PERSONAL_ACCESS_TOKEN environment variable is not set. " +
                "Please set it before running the application."
            );
        }

        StdioClientTransport? transport = null;
        try
        {
            //Console.WriteLine("DEBUG: Starting StdioClientTransportOptions creation");
            var options = new StdioClientTransportOptions
            {
                Command = "npx",
                Arguments =
                [
                    "-y",
                    "@modelcontextprotocol/server-github"
                ],
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    { "GITHUB_PERSONAL_ACCESS_TOKEN", token }
                }
            };
            //Console.WriteLine($"DEBUG: Options created successfully. Command: {options.Command}");

            //Console.WriteLine("DEBUG: Starting StdioClientTransport constructor");
            transport = new StdioClientTransport(options);
            //Console.WriteLine("DEBUG: StdioClientTransport created successfully");
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"DEBUG: Exception during transport creation: {ex.GetType().Name}");
            //Console.WriteLine($"DEBUG: Exception message: {ex.Message}");
            //Console.WriteLine($"DEBUG: Exception stack trace: {ex.StackTrace}");
            //if (ex.InnerException != null)
            //{
            //    Console.WriteLine($"DEBUG: Inner exception: {ex.InnerException.Message}");
            //    Console.WriteLine($"DEBUG: Inner stack trace: {ex.InnerException.StackTrace}");
            //}
            throw;
        }

        if (transport == null)
        {
            throw new InvalidOperationException("StdioClientTransport was not initialized properly (null after construction)");
        }

        var client =
            await McpClient.CreateAsync(
                transport
            );


        return client;
    }
}