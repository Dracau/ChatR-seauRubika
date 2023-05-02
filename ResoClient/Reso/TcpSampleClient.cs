using System.Net.Sockets;

public class TcpSampleClient
{
    private string pseudo;
    public async Task Run()
    {
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 666);

        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, leaveOpen: true);
        using var writer = new StreamWriter(stream, leaveOpen: true);
        
        EnterPseudo(writer);
        
        async Task DisplayLines()
        {
            if(pseudo == default) return;
            var newLine = await reader.ReadLineAsync();

            while(newLine != null)
            {
                switch (newLine[^1])
                {
                    case '0':
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case '1':
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case '2':
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        break;
                    case '3':
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                }

                newLine = newLine.Remove(newLine.Length-1, 1);
                Console.WriteLine(newLine);
                Console.ForegroundColor = ConsoleColor.White;
                newLine = await reader.ReadLineAsync();
            }
        }

        _=DisplayLines();
        
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Type a line to send to the server");
        Console.ForegroundColor = ConsoleColor.White;
        
        while (true)
        {
            var lineToSend = Console.ReadLine();
            Console.SetCursorPosition(0,Console.GetCursorPosition().Top-1);
            writer.WriteLine(lineToSend);
            await writer.FlushAsync();
        }
    }

    private async void EnterPseudo(StreamWriter writer)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Write your pseudo");
        Console.ForegroundColor = ConsoleColor.White;
        
        pseudo = Console.ReadLine();
        writer.WriteLine(pseudo);
        await writer.FlushAsync();
        Console.Clear();
        
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Welcome {pseudo} !");
        Console.ForegroundColor = ConsoleColor.White;
    }
}