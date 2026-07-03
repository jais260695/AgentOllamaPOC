namespace AgentOllamaPOC.Infrastructure;

public sealed class PromptService
{
    private readonly Dictionary<string, string> _cache = new();

    public string GetPrompt(string promptFile)
    {
        if (_cache.TryGetValue(promptFile, out var prompt))
        {
            return prompt;
        }

        var path = Path.Combine(AppContext.BaseDirectory,"Prompts",promptFile);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Prompt file not found: {path}");
        }

        prompt = File.ReadAllText(path);

        _cache[promptFile] = prompt;

        return prompt;
    }

    public void Reload(string promptFile)
    {
        _cache.Remove(promptFile);
    }

    public void ReloadAll()
    {
        _cache.Clear();
    }
}