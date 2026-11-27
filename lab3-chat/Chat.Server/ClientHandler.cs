using System.Net.Sockets;
using System.Text;
using Chat.Common;

namespace Chat.Server;

public class ClientHandler
{
    private const int MaxTextLength = 2000;
    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    private readonly TcpClient _tcp;
    private readonly ChatServer _server;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly object _sendLock = new();

    public string Nick { get; set; } = "";

    public ClientHandler(TcpClient tcp, ChatServer server)
    {
        _tcp = tcp;
        _server = server;
        var stream = tcp.GetStream();
        _reader = new StreamReader(stream, Utf8);
        _writer = new StreamWriter(stream, Utf8) { AutoFlush = true, NewLine = "\n" };
    }

    public bool Send(ChatMessage message)
    {
        lock (_sendLock)
        {
            try { _writer.WriteLine(message.ToJson()); return true; }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException) { return false; }
        }
    }

    public void Run()
    {
        try
        {
            var login = ChatMessage.FromJson(_reader.ReadLine() ?? "");
            if (login is not { Type: "login" })
            {
                Send(new ChatMessage("login_error", Text: "Ожидался вход"));
                return;
            }

            string nick = login.Nick.Trim();
            if (nick.Length is 0 or > 20)
            {
                Send(new ChatMessage("login_error", Text: "Ник должен быть от 1 до 20 символов"));
                return;
            }
            if (!_server.TryRegister(this, nick))
            {
                Send(new ChatMessage("login_error", Text: "Ник уже занят"));
                return;
            }

            Send(new ChatMessage("login_ok", nick));
            _server.SendHistory(this);
            _server.OnJoined(this);

            string? line;
            while ((line = _reader.ReadLine()) != null)
            {
                var m = ChatMessage.FromJson(line);
                if (m == null || string.IsNullOrWhiteSpace(m.Text)) continue;
                string text = m.Text.Length > MaxTextLength ? m.Text[..MaxTextLength] : m.Text;

                if (m.Type == "text") _server.PublishText(this, text);
                else if (m.Type == "private") _server.SendPrivate(this, m.To, text);
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException)
        {
        }
        finally
        {
            _server.Remove(this);
            try { _tcp.Close(); } catch { }
        }
    }
}
