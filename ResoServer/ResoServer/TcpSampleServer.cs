using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
public class TcpSampleServer
{
    private string logsPath = "logs.txt";
    
    private Dictionary<TcpClient, StreamWriter> _clients = new();
    private Dictionary<TcpClient, string> _clientsName = new();
    private Dictionary<TcpClient, int> _clientsRoom = new();
    public async Task Run()
    {
        var server = TcpListener.Create(666);
        server.Start();

        while (true)
        {
            var client = await server.AcceptTcpClientAsync();
            _ = Serve(client);
        }
    }

    private async Task Serve(TcpClient client)
    {
        try
        {
            using (client)
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, leaveOpen: true);
                using var writer = new StreamWriter(stream, leaveOpen: true);

                _clients.Add(client, writer);
                var nextLine = await reader.ReadLineAsync();
                while (nextLine != null)
                {
                    if (!FirstConnection(client, ref nextLine))
                    {
                        if (nextLine.StartsWith("/"))
                        {
                            ProcessCommand(client, nextLine);
                        }
                        else
                        {
                            foreach (var kvp in _clients)
                            {
                                kvp.Value.WriteLine("[" + DateTime.Now.ToString("H:mm:ss") + "] [" +
                                                    _clientsName[client] + "] " + nextLine + "1");
                                AddToLogs("[" + DateTime.Now.ToString("H:mm:ss") + "] [" + _clientsName[client] +
                                          "] " +
                                          nextLine + "1");
                                await kvp.Value.FlushAsync();
                            }
                        }
                    }

                    nextLine = await reader.ReadLineAsync();
                }
            }
        }
        catch (Exception)
        {
        }
        finally
        {
            _clients.Remove(client);
            _clientsName.Remove(client);
            if(_clientsRoom.ContainsKey(client)) _clientsRoom.Remove(client);
        }
    }

    private bool FirstConnection(TcpClient client, ref string nextLine)
    {
        if (!_clientsName.ContainsKey(client))
        {
            _clientsName.Add(client, nextLine);
            return true;
        }

        return false;
    }

    #region Commands

    private async Task ProcessCommand(TcpClient cl, string line)
    {
        string command = line.Substring(1);
        string[] commandParts = command.Split(' ');
        switch (commandParts[0])
        {
            case "broadcast":
                Broadcast(command.Substring(commandParts[0].Length + 1));
                break;
            case "close":
                CloseServer();
                break;
            case "all":
                String m2= "Personnes";
                foreach (var name in _clientsName)
                {
                    m2 += " - " + name.Value;
                    if (_clientsRoom.ContainsKey(name.Key))
                    {
                        m2 += " : " + _clientsRoom[name.Key];
                    }
                }
                _clients[cl].WriteLine("[" + DateTime.Now.ToString("H:mm:ss") + "] [Server] " + m2 + "0");
                AddToLogs("[" + DateTime.Now.ToString("H:mm:ss") + "] " + "["+ _clientsName[cl] +"] " + m2);
                await  _clients[cl].FlushAsync();
                break;
            case "mp":
                foreach (var client in _clientsName)
                {
                    if(client.Value.Equals(commandParts[1]))
                    {
                        String message= "";
                        for (int i = 2; i < commandParts.Length; i++)
                        {
                            message += commandParts[i] + " ";
                        }
                        
                        _clients[cl].WriteLine("[" + DateTime.Now.ToString("H:mm:ss") + "] TO [" + commandParts[1] + "] " + message + "2");
                        await  _clients[cl].FlushAsync();
                        _clients[client.Key].WriteLine("[" + DateTime.Now.ToString("H:mm:ss") + "] FROM [" + _clientsName[cl] + "] " + message +"2");
                        await  _clients[client.Key].FlushAsync();
                        AddToLogs("[" + DateTime.Now.ToString("H:mm:ss") + "] [" + commandParts[1] + "] " +
                                  message);
                        return;
                    }
                }
                _clients[cl].WriteLine("[" + DateTime.Now.ToString("H:mm:ss") + "] [Server] Aucun utilisateur ne s'apelle " + commandParts[1] +"0");
                await  _clients[cl].FlushAsync();
                return;
                break;
            case "r":
                if (!_clientsRoom.ContainsKey(cl))
                {
                    _clients[cl].WriteLine("[" + DateTime.Now.ToString("H:mm:ss") + "] [Server] Vous n'êtes dans aucune room0");
                    await  _clients[cl].FlushAsync();
                    return;
                }
                
                String message2= "";
                for (int i = 1; i < commandParts.Length; i++)
                {
                    message2 += commandParts[i] + " ";
                }
                
                foreach (var client in _clientsRoom)
                {
                    if (client.Value.Equals(_clientsRoom[cl]))
                    {
                        _clients[client.Key].WriteLine("[" + DateTime.Now.ToString("H:mm:ss") + "] ["+_clientsName[cl]+"] "+message2+"3");
                        await  _clients[client.Key].FlushAsync();
                    }
                }
                break;
            case "join":
                if (commandParts.Length < 2)
                {
                    _clients[cl].WriteLine("[" + DateTime.Now.ToString("H:mm:ss") + "] [Server] Veuillez entrer un numéro de room0");
                    await  _clients[cl].FlushAsync();
                    return;
                }else if (!int.TryParse(commandParts[1], out int result))
                {
                    _clients[cl].WriteLine("[" + DateTime.Now.ToString("H:mm:ss") + "] [Server] Veuillez entrer un numéro de room valide0");
                    await  _clients[cl].FlushAsync();
                    return;
                }
                
                
                _clientsRoom.Remove(cl);
                _clientsRoom.Add(cl, int.Parse(commandParts[1]));
                _clients[cl].WriteLine("[" + DateTime.Now.ToString("H:mm:ss") + "] [Server] Vous avez rejoind la room n°"+int.Parse(commandParts[1])+"0");
                await  _clients[cl].FlushAsync();
                break;
            default:
                Broadcast(commandParts[0] + " is not a valid command");
                break;
        }
    }

    private void Broadcast(string message)
    {
        foreach (var kvp in _clients)
        {
            kvp.Value.WriteLine("[" + DateTime.Now.ToString("H:mm:ss") + "] " + "[Server] " + message + "0");
            AddToLogs("[" + DateTime.Now.ToString("H:mm:ss") + "] " + "[Server] " + message + "0");
            kvp.Value.Flush();
        }
    }

    public void CloseServer()
    {
        Broadcast("[" + DateTime.Now.ToString("H:mm:ss") + "] Server is closing0");
        Environment.Exit(0);
    }

    string logs = "";

    public void AddToLogs(string message)
    {
        if (File.Exists(logsPath)) logs = File.ReadAllText(logsPath);
        File.WriteAllText(logsPath, logs + message + Environment.NewLine);
    }


    #endregion
}