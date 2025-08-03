namespace GameboyEmulator;

public class Logger
{
    private readonly string filePath;
    private readonly object sync = new();

    public Logger(string filePath)
    {
        this.filePath = Path.Combine(filePath, "log.txt");
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir); // ensure directory exists
        }
    }
    
    public void Log(string message)
    {
        lock (sync)
        {
            File.AppendAllText(filePath, message + Environment.NewLine);
        }
    }
}