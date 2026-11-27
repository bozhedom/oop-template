using System.Net;
using System.Net.Sockets;
using Chat.Common;

namespace Chat.Server;

public class ChatServer
{
    private const int HistorySize = 20;

    private readonly int _port;
    private readonly List<ClientHandler> _clients = new();
    private readonly object _lock = new(); 
    private readonly HistoryStore _history = new("history.jsonl");

    public ChatServer(int port) => _port = port;

    public void Start()
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        while (true)
        {
            TcpClient tcp = listener.AcceptTcpClient();
            var handler = new ClientHandler(tcp, this);
            new Thread(handler.Run) { IsBackground = true }.Start();
        }
    }

    public bool TryRegister(ClientHandler handler, string nick)
    {
        lock (_lock)
        {
            if (_clients.Any(c => string.Equals(c.Nick, nick, StringComparison.OrdinalIgnoreCase)))
                return false;
            handler.Nick = nick;
            _clients.Add(handler);
            return true;
        }
    }

    public void SendHistory(ClientHandler to)
    {
        foreach (var m in _history.Last(HistorySize))
            to.Send(m);
    }

    public void OnJoined(ClientHandler h)
    {
        Console.WriteLine($"+ {h.Nick}");
        Broadcast(new ChatMessage("join", h.Nick, Time: DateTime.UtcNow));
        BroadcastUsers();
    }

    public void Remove(ClientHandler h)
    {
        bool removed;
        lock (_lock) removed = _clients.Remove(h);
        if (!removed) return;
        Console.WriteLine($"- {h.Nick}");
        Broadcast(new ChatMessage("leave", h.Nick, Time: DateTime.UtcNow));
        BroadcastUsers();
    }

    public void PublishText(ClientHandler from, string text)
    {
        var msg = new ChatMessage("text", from.Nick, text, Time: DateTime.UtcNow);
        _history.Append(msg);
        Broadcast(msg);
    }

    public void SendPrivate(ClientHandler from, string toNick, string text)
    {
        ClientHandler? target;
        lock (_lock)
            target = _clients.FirstOrDefault(c => string.Equals(c.Nick, toNick, StringComparison.OrdinalIgnoreCase));

        if (target == null || target == from)
        {
            from.Send(new ChatMessage("error", Text: $"Пользователь «{toNick}» не в сети"));
            return;
        }
        var msg = new ChatMessage("private", from.Nick, text, target.Nick, DateTime.UtcNow);
        target.Send(msg);
        from.Send(msg); 
    }

    private void BroadcastUsers()
    {
        string[] nicks;
        lock (_lock) nicks = _clients.Select(c => c.Nick).ToArray();
        Broadcast(new ChatMessage("users", Users: nicks));
    }

    private void Broadcast(ChatMessage message)
    {
        ClientHandler[] snapshot;
        lock (_lock) snapshot = _clients.ToArray();
        foreach (var c in snapshot) c.Send(message);
    }
}
