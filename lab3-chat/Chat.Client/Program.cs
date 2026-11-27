using Chat.Client;

ApplicationConfiguration.Initialize();
while (true)
{
    using var login = new LoginForm();
    Application.Run(login);
    if (login.Client == null) break;
    Application.Run(new ChatForm(login.Client));
}
