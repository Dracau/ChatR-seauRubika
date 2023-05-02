using System.Net.Sockets;
using System.Text;

Console.WriteLine("Starting server");

var server = new TcpSampleServer();
await server.Run();