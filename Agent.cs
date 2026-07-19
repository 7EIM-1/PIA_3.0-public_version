namespace MCPClient;

public class Agent
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Prompt { get; set; }
    public string? skinpath = string.Empty;
    public string? voskpath = string.Empty;
    public List<string> mcp_Urls = new List<string>();
}
