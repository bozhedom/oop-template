using Chat.Common;

namespace Chat.Server;
public class HistoryStore
{
    private readonly string _path;
    private readonly object _lock = new();

    public HistoryStore(string path) => _path = path;

    public void Append(ChatMessage message)
    {
        lock (_lock)
        {
            try { File.AppendAllText(_path, message.ToJson() + "\n"); }
            catch (IOException) {}
        }
    }

    public List<ChatMessage> Last(int count)
    {
        lock (_lock)
        {
            if (!File.Exists(_path)) return new();
            try
            {
                return File.ReadAllLines(_path)
                    .TakeLast(count)
                    .Select(ChatMessage.FromJson)
                    .Where(m => m != null)
                    .Select(m => m!)
                    .ToList();
            }
            catch (IOException) { return new(); }
        }
    }
}
