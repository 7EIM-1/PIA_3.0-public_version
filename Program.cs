//This is the public Version of PIA - my Personal Intelligent Assistance
//please execuse any human error. I am somewhat trying to avoid AI slop. Also my english might be bad.. sorry.. i think...


//variables (i don't know how it is written, maybe something like this...)
using MCPClient;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

using ModelContextProtocol.Protocol;
using System.ClientModel;
using System.Collections;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using P.I.A_3._0_public_version;
using Vosk;

string project_path = Path.Combine(Environment.CurrentDirectory, "PIA");
string configpath = Path.Combine(project_path, "config.conf");
string Agentsfolder = Path.Combine(project_path, "Agents");
int AgentsIndex = -1;
CGUI ui = new CGUI();

Process? proc;   //arecord process

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
string? input = string.Empty;
var timeawait = TimeSpan.FromSeconds(300);  //text input timer
var listentime = 30;  //speech input timer in seconds

string trigger = "0";  //0 -> no trigger in use

//load configs:
while (!File.Exists(configpath))
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Configuration File not Found. Enter correct project path or leave blanc to generate new config file: ");
    string? input1 = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input1))
    {
        using (File.Create(Path.Combine(project_path, "config.conf"))) { }
    }
    else
    {
        try
        {
            Environment.CurrentDirectory = input1;
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
    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
        continue;

    string[] parts = line.Split(':', 2);

    if (parts.Length != 2)
        continue;

    string key = parts[0].Trim().ToLowerInvariant();
    string arg = parts[1].Trim();
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
string idleprompt = "collect as much context as possible and and start a conversation based on that context. if you can't find anything interesting just reply with something short like '...'.";
bool autolisten = false;

if (Directory.Exists(Agentsfolder))
{
    Console.WriteLine("loading agents...");
    LogAction("loading Agents...", project_path);
    foreach (string file in Directory.GetFiles(Agentsfolder))
    {
        List<string> urls = new List<string>();
        name = "";
        description = "";
        prompt = "";
        skinpath = "";
        vpath = "";
        idleprompt = "collect as much context as possible and and start a conversation based on that context. if you can't find anything interesting just reply with something short like '...'.";
        autolisten = false;

        if (file.EndsWith(".txt"))
        {
            Console.WriteLine($"Loading agent: {Path.GetFileNameWithoutExtension(file)}");
            LogAction($"Loading agent: {Path.GetFileNameWithoutExtension(file)}", project_path);
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
                }
                if (line.StartsWith("trigger:"))
                {
                    trigger = normal[8..].Trim().ToLower();
                    LogAction($"setting triggerword to {trigger}", project_path);
                }
                if (line.StartsWith("autolisten:"))
                {
                    if (normal[11..].Trim() == "true" || normal[11..].Trim() == "1")
                    {
                        autolisten = true;
                        Console.WriteLine(autolisten);
                    }
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
                        description += normal + "\n";
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
                        prompt += normal + "\n";
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
                    prompt = prompt.Replace("<prompt>", "");
                }
                if (line.StartsWith("idleprompt:"))
                {
                    idleprompt = normal[11..].Trim();
                }
                if (line.StartsWith("idletime:"))
                {
                    try
                    {
                        timeawait = TimeSpan.FromSeconds(int.Parse(normal[9..].Trim()));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        LogAction(ex.Message, project_path);
                    }

                }
                if (line.StartsWith("listentime:"))
                {
                    try
                    {
                        listentime = Int32.Parse(normal[11..]);
                    }
                    catch (Exception ex)
                    {
                        LogAction($"Error in file {file}: ex.Message",project_path);
                    }
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

            Agents.Add(new Agent { Name = name, Description = description, Prompt = prompt, mcp_Urls = urls, skinpath = skinpath, voskpath = vpath, autolisten = autolisten, idleprompt = idleprompt });
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
        input = Console.ReadLine();
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
    Agent selectedAgent = Agents[AgentsIndex];
    Console.WriteLine("Connecting to MCP servers...");

    // Create the MCP client(s).
    List<HttpClientTransport> transports = new List<HttpClientTransport>();         //creating a list of something
    foreach (var mcp in selectedAgent.mcp_Urls)
    {
        if (!string.IsNullOrWhiteSpace(mcp))
        {
            try
            {
                var uri = new Uri(mcp);
                transports.Add(new HttpClientTransport(new() { Endpoint = uri }));
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"MCP URL accepted: {mcp}");
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
    LogAction("loading skins...", project_path);
    string selectedSkinPath = selectedAgent.skinpath ?? string.Empty;
    LogAction(selectedSkinPath, project_path);
    ui.loadskins(selectedSkinPath);
}
try
{
    // --- arecord Setup ---
    var psi = new ProcessStartInfo
    {
        FileName = "arecord",
        Arguments = "-q -t raw -f S16_LE -r 16000 -c 1",
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
    LogAction(ex.Message, project_path);
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
bool x = false;
input = string.Empty;
while (true)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Green;
    if (AgentsIndex != -1 && Agents[AgentsIndex].skinpath != string.Empty)
    {
        if (!Agents[AgentsIndex].autolisten || x)
        {
            //input = ui.await_input().Trim();
            ui.printlisten();
            Console.WriteLine();
            input = await Input("You: ", timeawait);
            if (string.IsNullOrWhiteSpace(input))
            {
                input = Agents[AgentsIndex].idleprompt;
                Console.WriteLine("No input received. Using the default context-gathering prompt.");
            }
            x = false;
        }
        else
        {
            input = "#speak";
        }

        LogAction(input ?? "Empty user input", project_path);
    }
    else
    {
        input = await Input("You: ", timeawait);
        if (string.IsNullOrWhiteSpace(input))
        {
            if (AgentsIndex != -1)
            {
                input = Agents[AgentsIndex].idleprompt;
                Console.WriteLine("No input received. Using the default context-gathering prompt.");
            }
            else
            {
                continue;
            }
        }
        else
        {
            LogAction(input, project_path);
        }

    }
    if (input != null && input.StartsWith('#'))  //Commands
    {
        if (input == "#tools" || input == "#help")
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
            await Input("Press Enter", TimeSpan.FromSeconds(10));
            continue;   //skip the llm part
        }
        if (input == "#exit")
        {
            break;
        }
        if (input == "#speak")
        {
            Console.WriteLine("please speak now: ");
            input = string.Empty;
            input = await voskwatcher(listentime);
            if (trigger != "0" && !string.IsNullOrWhiteSpace(trigger) && !input.Contains(trigger))
            {
                input = string.Empty;
            }
            LogAction("STT returned: " + input, project_path);
            if (string.IsNullOrWhiteSpace(input))
            {
                x = true; // this disables autospeak so it will wait for a text prompt next
                LogAction("empty speech input. setting x to true", project_path);
            }
            else
            {
                bytesRead = 0;
            }
        }


    }
    if (input != null && input.Trim() == string.Empty)   //no input => just ignore
    {
        continue;
    }
    if (AgentsIndex != -1)
    {
        messages.Add(new(ChatRole.System, Agents[AgentsIndex].Prompt));
    }
    messages.Add(new(ChatRole.User, input));
    Console.ForegroundColor = ConsoleColor.Magenta;
    if (AgentsIndex != -1 && Agents[AgentsIndex].skinpath != string.Empty)
    {
        ui.say("Thinking...");
    }
    else
    { Console.WriteLine("Thinking..."); }
    string result = string.Empty;
    await foreach (ChatResponseUpdate update in client.GetStreamingResponseAsync(messages, new() { Tools = [.. tools] }))
    {
        Console.Write(update.Text);
        result += update.Text;
        //cancel if esc
        if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
        {
            Console.WriteLine("Cancelling...");
            LogAction("llm response canceled by user", project_path);
            break;
        }

    }
    LogAction(result, project_path);

}


//functions

void LogAction(string message, string project_path)
{
    try
    {
        if (!File.Exists(Path.Combine(project_path, "logfile.log")))
        {
            using (File.Create(Path.Combine(project_path, "logfile.log"))) { }
            ;
        }
        File.AppendAllText(Path.Combine(project_path, "logfile.log"), DateTime.Now + " -> " + message + "\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        Console.WriteLine();
    }
}

async Task<string> voskwatcher(int timeout = 30)
{
    bytesRead = 0;

    var tout = TimeSpan.FromSeconds(timeout);
    var sw = Stopwatch.StartNew();

    while (sw.Elapsed < tout)
    {
        try
        {
            if (proc != null &&
                proc.StandardOutput != null &&
                !proc.StandardOutput.EndOfStream &&
                recognizer != null)
            {
                bytesRead = proc.StandardOutput.BaseStream.Read(
                    buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    if (recognizer.AcceptWaveform(buffer, bytesRead))
                    {
                        string result = recognizer.Result();
                        string text = Extract(result, "text");

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            Console.WriteLine($"Recognized: {text}");
                            return text;
                        }
                    }
                    else
                    {
                        string partial = recognizer.PartialResult();
                        Console.WriteLine($"Partial: {Extract(partial, "partial")}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }

        await Task.Delay(20);
    }

    // Get anything Vosk has accumulated before timing out.
    if (recognizer != null)
    {
        string finalResult = recognizer.FinalResult();
        string text = Extract(finalResult, "text");

        if (!string.IsNullOrWhiteSpace(text))
            return text;
    }

    return string.Empty;
}
string Extract(string json, string field)
{
    try
    {
        return JsonDocument.Parse(json).RootElement.GetProperty(field).GetString() ?? string.Empty;
    }
    catch
    {
        return "";
    }
}

async Task<string> Input(string prompt, TimeSpan timeout)
{
    Console.Write(prompt);

    // Console.ReadLine() blocks forever, so it cannot be used directly when a
    // timeout is required. Read individual keys instead and check the timeout
    // between them. This also avoids leaving a background ReadLine task behind
    // when the timeout expires.
    if (Console.IsInputRedirected)
    {
        // KeyAvailable/ReadKey are only available for an interactive console.
        // This fallback is useful when input is piped into the application.
        return await Task.Run(() => Console.ReadLine() ?? string.Empty);
    }

    var line = new StringBuilder();
    var stopwatch = Stopwatch.StartNew();

    while (stopwatch.Elapsed < timeout)
    {
        while (Console.KeyAvailable)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return line.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (line.Length > 0)
                {
                    line.Length--;
                    Console.Write("\b \b");
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                line.Append(key.KeyChar);
                Console.Write(key.KeyChar);
            }
        }

        await Task.Delay(50);
    }

    Console.WriteLine();
    return string.Empty;
}