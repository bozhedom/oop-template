using Chat.Common;

namespace Chat.Client;

public class ChatForm : Form
{
    private readonly ChatClient _client;
    private readonly RichTextBox _feed = new() { ReadOnly = true, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
    private readonly ListBox _users = new();
    private readonly Label _onlineLabel = new() { Text = "Онлайн (0)", AutoSize = true };
    private readonly TextBox _input = new() { PlaceholderText = "Сообщение... (Enter — отправить)" };
    private readonly Button _send = new() { Text = "Отправить" };
    private readonly CheckBox _private = new() { Text = "Лично выбранному", AutoSize = true, Enabled = false };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Color.Gray };
    private readonly Font _bold, _italic;

    private static readonly Color[] Palette =
    {
        Color.RoyalBlue, Color.Chocolate, Color.SeaGreen, Color.MediumPurple,
        Color.Crimson, Color.Teal, Color.DarkGoldenrod, Color.DeepPink
    };

    public ChatForm(ChatClient client)
    {
        _client = client;
        Text = $"Чат — {client.Nick}";
        ClientSize = new Size(700, 480);
        MinimumSize = new Size(500, 350);
        StartPosition = FormStartPosition.CenterScreen;

        _bold = new Font(_feed.Font, FontStyle.Bold);
        _italic = new Font(_feed.Font, FontStyle.Italic);

        _feed.SetBounds(10, 10, 480, 375);
        _feed.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        _onlineLabel.Location = new Point(500, 10);
        _users.SetBounds(500, 30, 190, 355);
        _users.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
        _users.SelectedIndexChanged += (_, _) => UpdatePrivateCheckbox();

        _input.SetBounds(10, 395, 480, 25);
        _input.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _send.SetBounds(500, 393, 190, 28);
        _send.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _send.Click += (_, _) => SendCurrent();
        AcceptButton = _send;

        _private.Location = new Point(10, 428);
        _private.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _status.Location = new Point(10, 455);
        _status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _status.Text = "Подключено";

        Controls.AddRange(new Control[] { _feed, _onlineLabel, _users, _input, _send, _private, _status });

        _client.MessageReceived += OnMessage;
        _client.Disconnected += OnDisconnected;
        Shown += (_, _) => { _client.StartReading(); _input.Focus(); };
        FormClosing += (_, _) =>
        {
            _client.MessageReceived -= OnMessage;
            _client.Disconnected -= OnDisconnected;
            _client.Disconnect();
        };
    }


    private void OnMessage(ChatMessage m) => RunOnUi(() => Handle(m));

    private void OnDisconnected() => RunOnUi(() =>
    {
        AppendLine("", "Соединение с сервером потеряно", Color.Firebrick, italic: true);
        _status.Text = "Нет соединения";
        _send.Enabled = _input.Enabled = false;
    });

    private void RunOnUi(Action action)
    {
        if (IsDisposed || !IsHandleCreated) return;
        try { BeginInvoke(action); }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException) { }
    }


    private void Handle(ChatMessage m)
    {
        string time = m.Time == default ? "" : m.Time.ToLocalTime().ToString("HH:mm");
        switch (m.Type)
        {
            case "text":
                AppendChat(time, m.Nick, m.Text, back: null);
                break;
            case "private":
                bool mine = m.Nick == _client.Nick;
                string label = mine ? $"вы → {m.To} лично" : $"{m.Nick} → вам лично";
                AppendChat(time, label, m.Text, Color.LemonChiffon, ColorFor(m.Nick));
                break;
            case "join":
                AppendLine(time, $"{m.Nick} вошёл в чат", Color.Gray, italic: true);
                break;
            case "leave":
                AppendLine(time, $"{m.Nick} вышел из чата", Color.Gray, italic: true);
                break;
            case "users":
                UpdateUsers(m.Users ?? Array.Empty<string>());
                break;
            case "error":
                AppendLine("", m.Text, Color.Firebrick, italic: true);
                break;
        }
    }

    private void UpdateUsers(string[] nicks)
    {
        string? selected = _users.SelectedItem as string;
        _users.BeginUpdate();
        _users.Items.Clear();
        foreach (var n in nicks) _users.Items.Add(n);
        if (selected != null && _users.Items.Contains(selected)) _users.SelectedItem = selected;
        _users.EndUpdate();
        _onlineLabel.Text = $"Онлайн ({nicks.Length})";
        UpdatePrivateCheckbox();
    }

    private void UpdatePrivateCheckbox()
    {
        bool canPm = _users.SelectedItem is string s && s != _client.Nick;
        _private.Enabled = canPm;
        if (!canPm) _private.Checked = false;
        _private.Text = canPm ? $"Лично: {_users.SelectedItem}" : "Лично выбранному";
    }

    private void SendCurrent()
    {
        string text = _input.Text.Trim();
        if (text.Length == 0) return;

        ChatMessage msg = _private.Checked && _users.SelectedItem is string to
            ? new ChatMessage("private", Text: text, To: to)
            : new ChatMessage("text", Text: text);

        if (!_client.Send(msg))
        {
            AppendLine("", "Не удалось отправить: нет соединения", Color.Firebrick, italic: true);
            return;
        }
        _input.Clear();
        _input.Focus();
    }

    private static Color ColorFor(string nick) =>
        Palette[(nick.Sum(c => c) & 0x7fffffff) % Palette.Length];

    private void AppendChat(string time, string nickLabel, string text, Color? back, Color? nickColor = null)
    {
        AppendPiece(time + " ", Color.Gray, _feed.Font, back);
        AppendPiece(nickLabel + ": ", nickColor ?? ColorFor(nickLabel), _bold, back);
        AppendPiece(text + "\n", Color.Black, _feed.Font, back);
        _feed.ScrollToCaret();
    }

    private void AppendLine(string time, string text, Color color, bool italic)
    {
        if (time.Length > 0) AppendPiece(time + " ", Color.Gray, _feed.Font, null);
        AppendPiece(text + "\n", color, italic ? _italic : _feed.Font, null);
        _feed.ScrollToCaret();
    }

    private void AppendPiece(string s, Color color, Font font, Color? back)
    {
        _feed.SelectionStart = _feed.TextLength;
        _feed.SelectionLength = 0;
        _feed.SelectionColor = color;
        _feed.SelectionFont = font;
        _feed.SelectionBackColor = back ?? _feed.BackColor;
        _feed.AppendText(s);
    }
}
