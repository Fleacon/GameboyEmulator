using System.Text;

namespace GameboyEmulator;

public class Logger
{
    private readonly StringBuilder buffer;
    private readonly string filePath;
    private bool startLine;
    private const int BUFFERSIZE = 10000;

    public Logger(string filePath)
    {
        this.filePath = Path.Combine(filePath, "log.txt");
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir); // ensure directory exists
        }
        
        buffer = new StringBuilder();
    }
    
    public void Log(string message)
    {
        if (!startLine)
        {
            buffer.AppendLine("A:01 F:B0 B:00 C:13 D:00 E:D8 H:01 L:4D SP:FFFE PC:0100 PCMEM:00,C3,13,02");
            startLine = true;
        }
        buffer.AppendLine(message);
        if (buffer.Length >= BUFFERSIZE)
        {
            SaveToFile();
            buffer.Clear();
        }
    }

    public void SaveToFile()
    {
        File.AppendAllText(filePath, buffer.ToString());
    }

    public void RegisterShutdownHandler()
    {
        AppDomain.CurrentDomain.ProcessExit += (sender, args) => SaveToFile();
        Console.CancelKeyPress += (sender, args) => SaveToFile();
    }
}