namespace AgentOllamaPOC.Models;

public class DocumentChunk
{
    public string FileName { get; set; } = "";

    public string Content { get; set; } = "";

    public float[] Vector { get; set; } = [];
}
