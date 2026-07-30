using System;
using System.Runtime.CompilerServices;

namespace P.I.A_3._0_public_version;

public class CGUI
{

    List<List<string>> skin = new List<List<string>>{
        new List<string>(),
        new List<string>()
    };

    public void loadskins(string skinpath)
    {
        if(File.Exists(skinpath ) && skinpath.EndsWith(".txt"))
        {
            string[] lines = File.ReadAllLines(skinpath);
            for(int i = 0; i < lines.Count(); i++)
            {
                Console.WriteLine(lines[i]);
                if(lines[i].Trim().ToLower() == "[listen]>>")
                {
                    i++;
                    while (lines[i].Trim().ToLower() != "<<[listen]" && i < lines.Count())
                    {
                        
                        skin[0].Add(lines[i]);
                        Console.WriteLine(lines[i]);
                        i++;
                    }
                }
                if (lines[i].Trim().ToLower() == "[speak]>>")
                {
                    i++;
                    while (lines[i].Trim().ToLower() != "<<[speak]" && i < lines.Count())
                    {
                        skin[1].Add(lines[i]);
                        Console.WriteLine(lines[i]);
                        i++;
                    }
                }
            }
        }
    }

    public string await_input()
    {
        try{
        foreach(var line in skin[0])
        {
            Console.WriteLine(line);
        }
        Console.WriteLine();
        return Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return ex.Message;
        }
    }
    public void say(string text, int a = 50)
    {
        int time = 0;
        time = text.Length*a;
        foreach(string line in skin[1])
        {
            Console.WriteLine(line);
        }
        Console.WriteLine(text);
        Thread.Sleep(time);
    }

    public void printspeak()
    {
        foreach (string line in skin[1])
        {
            Console.WriteLine(line);
        }
    }

    public void printlisten()
    {
        foreach (string line in skin[0])
        {
            Console.WriteLine(line);
        }
    }
}
