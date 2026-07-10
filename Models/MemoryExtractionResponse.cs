using AgentOllamaPOC.Memory.Models;

namespace AgentOllamaPOC.Models;

public sealed class MemoryExtractionResponse
{
    public List<ExtractedMemory> Memories { get; set; } = [];
}