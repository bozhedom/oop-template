namespace Chat.Client;

public class LoginForm : Form
{
    private readonly TextBox _host = new() { Text = "127.0.0.1" };
    private readonly TextBox _port = new() { Text = "5000" };
    private readonly TextBox _nick = new();
    private readonly Button _login = new() { Text = "Войти" };
    private readonly Label _error = new() { ForeColor = Color.Firebrick };

    public ChatClient? Client { get; private set; }

    public LoginForm()
    {
        Text = "Вход в чат";
        ClientSize = new Size(300, 270);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        AddField("Сервер", _host, 10);
        AddField("Порт", _port, 70);
        AddField("Ник", _nick, 130);

        _login.SetBounds(200, 190, 90, 28);
        _login.Click += OnLogin;
        _error.SetBounds(10, 225, 280, 40);
        Controls.AddRange(new Control[] { _login, _error });
        AcceptButton = _login;
    }

    private void AddField(string caption, TextBox box, int y)
    {
        var label = new Label { Text = caption, AutoSize = true, Location = new Point(10, y) };
        box.SetBounds(10, y + 20, 280, 25);
        Controls.AddRange(new Control[] { label, box });
    }

    private async void OnLogin(object? sender, EventArgs e)
    {
        _error.Text = "";
        if (!int.TryParse(_port.Text, out int port) || port is < 1 or > 65535)
        {
            _error.Text = "Порт — число от 1 до 65535";
            return;
        }
        if (string.IsNullOrWhiteSpace(_nick.Text))
        {
            _error.Text = "Введите ник";
            return;
        }

        _login.Enabled = false;
        _error.ForeColor = Color.Gray;
        _error.Text = "Подключение...";

        var client = new ChatClient();
        string? error = await client.ConnectAsync(_host.Text.Trim(), port, _nick.Text.Trim());

        _error.ForeColor = Color.Firebrick;
        _login.Enabled = true;
        if (error != null)
        {
            _error.Text = error;
            return;
        }
        Client = client;
        Close();
    }
}
