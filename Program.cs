//This is the public Version of PIA - my Personal Intelligent Assistance
//please execuse any human error. I am trying to not rely on AI slop. Also my english might be bad.. sorry.. i think...


//variables (i don't know how it is written, maybe something like this...)
using MCPClient;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

using ModelContextProtocol.Protocol;
using System.ClientModel;
using System.Collections;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Runtime.CompilerServices;
using P.I.A_3._0_public_version;
using Vosk;

string project_path = Path.Combine(Environment.CurrentDirectory, "PIA");
string configpath = Path.Combine(project_path, "config.conf");
string Agentsfolder = Path.Combine(project_path, "Agents");
int AgentsIndex = -1;
CGUI ui = new CGUI();

Process proc;   //arecord process

List<Agent> Agents = new List<Agent>();
List<McpClient> MCPClients = new List<McpClient>();
List<ChatMessage> messages = [];
List<AITool> tools = [];
var clientOptions = new OpenAI.OpenAIClientOptions { Endpoint = new Uri("http://127.0.0.1:8080") };
var openAIClient = new OpenAI.OpenAIClient(new ApiKeyCredential("not-needed"), clientOptions);

//Vosk
string voskmodelpath = string.Empty;
proc = null;
byte[] buffer = new byte[4096];
int bytesRead;
var recognizer = (VoskRecognizer?)null;


//load configs:
while (!File.Exists(configpath))
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Configuration File not Found. Enter correct project path or leave blanc to generate new config file: ");
    string? Input = Console.ReadLine();
    if (Input == string.Empty)
    {
        File.Create(Path.Combine(project_path, "config.conf"));
    }
    else
    {
        try
        {
            if (Input != null)
                Environment.CurrentDirectory = Input;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
        }
    }
    //redefine paths
    project_path = Path.Combine(Environment.CurrentDirectory ?? ".", "PIA");
    configpath = Path.Combine(project_path, "config.conf");
    Agentsfolder = Path.Combine(project_path, "Agents");
    Console.ResetColor();
}

string[] configs = File.ReadAllLines(configpath);
foreach (var line in configs)
{
    string key = line.ToLower().Split(':')[0].Trim();
    string arg = line.Split(':')[1].Trim();
    LogAction($"setting {key} to {arg}...", project_path);
    try
    {
        if (key.Trim().StartsWith("#"))
        {
            continue;
        }
        if (key.Trim().StartsWith("agentsfolderpath"))
        {
            Agentsfolder = arg;
            Console.WriteLine(Agentsfolder);
        }
        if (key.Trim().StartsWith("voskmodel"))
        {
            voskmodelpath = arg;
        }

    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(ex.Message);
        LogAction(ex.Message, project_path);
        Console.ResetColor();
    }
}


//load Agents
string name = "";
string description = "";
string prompt = "";
string skinpath = "";
string vpath = ""; //path to vosk model
List<string> urls = new List<string>();

if (Directory.Exists(Agentsfolder))
{
    Console.WriteLine("loading agents...");
    LogAction("loading Agents...", project_path);
    foreach (string file in Directory.GetFiles(Agentsfolder))
    {
        name = "";
        description = "";
        prompt = "";
        skinpath = "";
        vpath = "";
        if (file.EndsWith(".txt"))
        {
            Console.WriteLine($"Loading agent: {file.TrimEnd(".txt").TrimStart(Agentsfolder)}");
            LogAction($"Loading agent: {file.TrimEnd(".txt").TrimStart(Agentsfolder)}", project_path);
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].ToLower().Trim();
                string normal = lines[i];
                Console.WriteLine(line);
                if (line.StartsWith("#"))
                {
                    continue;
                }
                if (line.StartsWith("name:"))
                {
                    name = line[5..].Trim();
                    Console.WriteLine("Name: " + name);
                }
                if (line.StartsWith("skinpath:"))
                {
                    skinpath = normal[9..].Trim();
                    Console.WriteLine("skin: " + skinpath);
                    Console.ReadLine();
                }
                if (line.StartsWith("voskpath:"))
                {
                    vpath = normal[9..].Trim();
                }
                if (line == "<description>")
                {
                    description = "";
                    while (line != "</description>")
                    {
                        description += line + "\n";
                        i++;
                        if (i < lines.Length)
                        {
                            line = lines[i];
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                if (line == "<prompt>")
                {
                    prompt = "";
                    while (line != "</prompt>")
                    {
                        prompt += line + "\n";
                        i++;
                        if (i < lines.Length)
                        {
                            line = lines[i];
                        }
                        else
                        {
                            break;
                        }
                    }
                    prompt.Trim("<prompt>");
                }
                if (line == "<mcp_urls>" || line == "<mcpurls>")
                {
                    i++;
                    while (i < lines.Length)
                    {
                        line = lines[i].Trim();
                        if (line == "</mcp_urls>" || line == "</mcpurls>")
                        {
                            break;
                        }
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            urls.Add(line);
                        }
                        i++;
                    }
                }
            }

            Agents.Add(new Agent { Name = name, Description = description, Prompt = prompt, mcp_Urls = urls, skinpath = skinpath, voskpath = vpath });
        }
    }
    foreach (var agent in Agents)
    {
        Console.WriteLine($"Name: {agent.Name}\nDescription: {agent.Description}");
        Console.WriteLine();
    }
    bool valid = false;
    while (!valid)
    {
        Console.Write("please select an agent by entering a name: ");
        string? input = Console.ReadLine();
        if (input == string.Empty)
        {
            Console.WriteLine("No input, skipping Agent selection...");
            break;
        }
        else
        {
            for (int i = 0; i < Agents.Count(); i++)
            {
                if (Agents[i] != null && input != null && Agents[i].Name?.ToLower().Trim() == input.ToLower().Trim())
                {
                    AgentsIndex = i;
                    valid = true;
                    break;
                }
            }
        }
    }
}
else
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("No Agents found. Skipping...");
    Console.ResetColor();
}


//init
if (AgentsIndex != -1)
{
    Console.WriteLine("Connecting to MCP servers...");
    HttpClient httpClient = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8080") };

    // Create the MCP client(s).
    List<HttpClientTransport> transports = new List<HttpClientTransport>();         //creating a list of something
    foreach (var mcp in Agents[AgentsIndex]?.mcp_Urls ?? new List<string>())
    {
        if (!string.IsNullOrWhiteSpace(mcp))
        {
            try
            {
                var uri = new Uri(mcp);
                transports.Add(new HttpClientTransport(new() { Endpoint = uri }));
            }
            catch (UriFormatException ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Invalid MCP URL: {mcp} - {ex.Message}");
                Console.ResetColor();
            }
        }
    }


    // Create the MCP client(s) with proper error handling
    foreach (var transport in transports)
    {
        try
        {
            McpClient mcpClient = await McpClient.CreateAsync(transport);
            MCPClients.Add(mcpClient);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Failed to create MCP client: {ex.Message}");
            Console.ResetColor();
            LogAction($"Failed to create MCP client: {ex.Message}", project_path);
            continue;
        }
    }
    LogAction("loading skins...",project_path);
    LogAction(Agents[AgentsIndex].skinpath,project_path);
    ui.loadskins(Agents[AgentsIndex].skinpath);
}
try
{
    // --- arecord Setup ---
    var psi = new ProcessStartInfo
    {
        FileName = "arecord",
        Arguments = "-f S16_LE -r 16000 -c 1",   // 16-bit PCM, 16 kHz, Mono
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };
    proc = Process.Start(psi);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine(ex.Message);
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Make sure that arecord is installed!");
    LogAction(ex.Message,project_path);
    Console.ResetColor();
}

try
{
    Vosk.Vosk.GpuInit();
    Console.WriteLine("Initializing Vosk speech recognizer...");
    Vosk.Vosk.SetLogLevel(0);
    var model = new Model(voskmodelpath);
    recognizer = new VoskRecognizer(model, 16000f);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Error initializing Vosk recognizer: " + ex.Message);
    LogAction("Error initializing Vosk recognizer: " + ex.Message, project_path);
    Console.WriteLine("Speech recognition will not be available. This is likely due to an incorrect STT path argument or an issue with the Vosk model file(s).");
    Console.ResetColor();
}

foreach (var mcpClient in MCPClients)
{   // List all available tools from the MCP servers.
    try
    {
        Console.WriteLine("Available tools:\n");
        IList<McpClientTool> mcpTools = await mcpClient.ListToolsAsync();
        foreach (McpClientTool tool in mcpTools)
        {
            Console.WriteLine($"{tool}");
            tools.Add(tool);
        }
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Failed to list tools: {ex.Message}");
        Console.ResetColor();
        LogAction($"Failed to list tools: {ex.Message}", project_path);
    }
}

IChatClient client =
    new ChatClientBuilder(openAIClient.GetChatClient("model").AsIChatClient())
    .UseFunctionInvocation()
    .Build();

//Main loop

while (true)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Green;
    string? input;
    //Console.WriteLine(Agents[AgentsIndex].skinpath);
    //Console.ReadLine();
    if (AgentsIndex != -1 && Agents[AgentsIndex].skinpath != string.Empty )
    {
        input = ui.await_input().Trim();
        LogAction(input, project_path);
    }
    else
    {
        Console.Write("You: ");
        input = Console.ReadLine();
    }
    if (input != null && input.StartsWith('#'))  //Commands
    {
        if (input == "#tools")
        {
            foreach (var mcpClient in MCPClients)
            {   // List all available tools from the MCP servers.
                try
                {
                    Console.WriteLine("Available tools:\n");
                    IList<McpClientTool> mcpTools = await mcpClient.ListToolsAsync();
                    foreach (McpClientTool tool in mcpTools)
                    {
                        Console.WriteLine($"{tool}");
                        tools.Add(tool);
                    }
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Failed to list tools: {ex.Message}");
                    Console.ResetColor();
                    LogAction($"Failed to list tools: {ex.Message}", project_path);
                }
            }
        }
        if(input == "#exit")
        {
            break;
        }

        continue;
    }
    else if (input != null && input.Trim() == string.Empty)   //no input => just ignore
    {
        continue;
    }
    else                        //no command => handle as message for llm
    {
        if(AgentsIndex != -1)
        {
            messages.Add(new(ChatRole.System, Agents[AgentsIndex].Prompt));
        }
        messages.Add(new(ChatRole.User, input));
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("Thinking...");
        List<ChatResponseUpdate> updates = [];
        string response = string.Empty;
        await foreach (ChatResponseUpdate update in client.GetStreamingResponseAsync(messages, new() { Tools = [.. tools] }))
        {
            Console.Write(update.Text);
            updates.Add(update);
            response += update;
            //cancel if esc
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
            {
                Console.WriteLine("Cancelling...");
                break;
            }

        }
    }

}


//functions

void LogAction(string message, string project_path)
{
    try
    {
        if (!File.Exists(Path.Combine(project_path, "logfile.log")))
        {
            File.Create(Path.Combine(project_path, "logfile.log"));
        }
        File.AppendAllText(Path.Combine(project_path, "logfile.log"), DateTime.Now + " -> " + message + "\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        Console.WriteLine();
    }
}

async Task<string> voskwatcher()
{
    while (true)
    {
        try
        {
            if (proc.StandardOutput != null && !proc.StandardOutput.EndOfStream)
            {
                bytesRead = proc.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    if (recognizer.AcceptWaveform(buffer, bytesRead))
                    {
                        string result = recognizer.Result();
                        string text = Extract(result, "text");
                        if (!string.IsNullOrEmpty(text))
                        {
                            Console.WriteLine($"Recognized: {text}");
                            //input = text;
                            return text;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}

string Extract(string json, string field)
{
    try
    {
        return JsonDocument.Parse(json).RootElement.GetProperty(field).GetString();
    }
    catch
    {
        return "";
    }
}