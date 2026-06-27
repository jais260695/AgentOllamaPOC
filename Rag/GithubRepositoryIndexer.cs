using AgentOllamaPOC.Rag;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System.Text.Json;


namespace AgentOllamaPOC.Rag;


public class GithubRepositoryIndexer
{

    private readonly McpClient _mcpClient;

    private readonly RagService _ragService;

    private readonly ILogger<GithubRepositoryIndexer> _logger;



    private readonly string[] _extensions =
    [
        ".cs",
        ".md",
        ".json",
        ".xml",
        ".yml",
        ".yaml"
    ];



    public GithubRepositoryIndexer(

        McpClient mcpClient,

        RagService ragService,

        ILogger<GithubRepositoryIndexer> logger

    )
    {

        _mcpClient = mcpClient;

        _ragService = ragService;

        _logger = logger;

    }





    public async Task IndexRepositoryAsync(

        string owner,

        string repo,

        string branch,

        CancellationToken token

    )
    {


        _logger.LogInformation(
            "Starting indexing {Owner}/{Repo} branch {Branch}",
            owner,
            repo,
            branch
        );



        var files =
            await GetFilesRecursiveAsync(

                owner,

                repo,

                "",

                branch,

                token

            );



        _logger.LogInformation(
            "Total files discovered: {Count}",
            files.Count
        );



        foreach (var file in files)
        {

            token.ThrowIfCancellationRequested();



            _logger.LogInformation(
                "Indexing file {File}",
                file
            );



            var result =
                await _mcpClient
                .CallToolAsync(

                    "get_file_contents",

                    new Dictionary<string, object?>
                    {

                        ["owner"] = owner,

                        ["repo"] = repo,

                        ["path"] = file,

                        ["branch"] = branch

                    }

                );



            var content =
                result.Content
                .FirstOrDefault()
                ?.ToString();



            if (string.IsNullOrWhiteSpace(content))
            {

                _logger.LogWarning(
                    "Empty content for {File}",
                    file
                );

                continue;
            }



            await _ragService
                .IndexDocumentAsync(

                    file,

                    content

                );


        }



        _logger.LogInformation(
            "Completed indexing {Owner}/{Repo}",
            owner,
            repo
        );

    }







    private async Task<List<string>> GetFilesRecursiveAsync(

    string owner,

    string repo,

    string path,

    string branch,

    CancellationToken token

)
    {

        var files =
            new List<string>();


        token.ThrowIfCancellationRequested();



        _logger.LogInformation(
            "Scanning directory {Path}",
            string.IsNullOrEmpty(path)
                ? "/"
                : path
        );



        var result =
            await _mcpClient.CallToolAsync(

                "get_file_contents",

                new Dictionary<string, object?>
                {
                    ["owner"] = owner,

                    ["repo"] = repo,

                    ["path"] = path,

                    ["branch"] = branch
                }

            );



        foreach (var item in result.Content)
        {

            var json =
                item.ToString();



            if (string.IsNullOrWhiteSpace(json))
                continue;



            try
            {

                using var doc =
                    JsonDocument.Parse(json);



                // MCP returns array
                if (doc.RootElement.ValueKind
                    != JsonValueKind.Array)
                {
                    continue;
                }



                foreach (var element in doc.RootElement.EnumerateArray())
                {


                    var type =
                        element
                        .GetProperty("type")
                        .GetString();



                    var itemPath =
                        element
                        .GetProperty("path")
                        .GetString();



                    if (string.IsNullOrEmpty(itemPath))
                        continue;



                    if (type == "dir")
                    {


                        _logger.LogInformation(
                            "Entering folder {Folder}",
                            itemPath
                        );



                        var nestedFiles =
                            await GetFilesRecursiveAsync(

                                owner,

                                repo,

                                itemPath,

                                branch,

                                token

                            );


                        files.AddRange(
                            nestedFiles
                        );

                    }


                    else if (type == "file")
                    {


                        if (IsSupported(itemPath))
                        {

                            files.Add(
                                itemPath
                            );


                            _logger.LogInformation(
                                "Found file {File}",
                                itemPath
                            );

                        }

                    }

                }


            }
            catch (Exception ex)
            {

                _logger.LogError(
                    ex,
                    "Failed parsing MCP response"
                );

            }

        }



        return files;

    }







    private bool IsSupported(
        string path)
    {

        return _extensions
            .Any(x =>
                path.EndsWith(
                    x,
                    StringComparison.OrdinalIgnoreCase
                ));

    }

}