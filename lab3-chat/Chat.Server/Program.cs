using System.Text;
using Chat.Server;

Console.OutputEncoding = Encoding.UTF8;
int port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 5000;
Console.WriteLine($"Сервер чата слушает порт {port}. Остановка: Ctrl+C.");
new ChatServer(port).Start();
