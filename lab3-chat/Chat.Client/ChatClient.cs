using System.Net.Sockets;
using System.Text;
using Chat.Common;

namespace Chat.Client;

public class ChatClient
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    private TcpClient? _tcp;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly object _sendLock = new();
    private volatile bool _closedByUser;

    public event Action<ChatMessage>? MessageReceived;
    public event Action? Disconnected;

    public string Nick { get; private set; } = "";

    public async Task<string?> ConnectAsync(string host, int port, string nick)
    {
        try
        {
            _tcp = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _tcp.ConnectAsync(host, port, cts.Token);

            var stream = _tcp.GetStream();
            _reader = new StreamReader(stream, Utf8);
            _writer = new StreamWriter(stream, Utf8) { AutoFlush = true, NewLine = "\n" };

            await _writer.WriteLineAsync(new ChatMessage("login", nick).ToJson());
            string? line = await _reader.ReadLineAsync(cts.Token);
            var reply = line == null ? null : ChatMessage.FromJson(line);

            if (reply?.Type == "login_ok")
            {
                Nick = reply.Nick;
                return null;
            }
            Disconnect();
            return string.IsNullOrEmpty(reply?.Text) ? "Сервер не принял вход" : reply.Text;
        }
        catch (Exception ex) when (ex is SocketException or IOException or OperationCanceledException or ArgumentException)
        {
            Disconnect();
            return "Сервер недоступен. Проверьте адрес и порт.";
        }
    }
    public void StartReading() => new Thread(ReadLoop) { IsBackground = true }.Start();

    public bool Send(ChatMessage message)
    {
        lock (_sendLock)
        {
            try { _writer!.WriteLine(message.ToJson()); return true; }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or NullReferenceException) { return false; }
        }
    }

    public void Disconnect()
    {
        _closedByUser = true;
        try { _tcp?.Close(); } catch { }
    }

    private void ReadLoop()
    {
        try
        {
            string? line;
            while ((line = _reader!.ReadLine()) != null)
            {
                var m = ChatMessage.FromJson(line);
                if (m != null) MessageReceived?.Invoke(m);
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException) { }

        if (!_closedByUser) Disconnected?.Invoke();
    }
}
