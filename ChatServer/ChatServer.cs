using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class CurrentClientInfo
{
    public CurrentClientInfo(ClientsInfo clientsInfo, Guid currentClientGuid)
    {
        AllClients = clientsInfo;
        guid = currentClientGuid;
    }

    public ClientsInfo AllClients { get; set; }
    public Guid guid { get; set; }
}

class Program
{

    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            ShowUsage();
            return;
        }
        if (!IPAddressParser.Parse(args[0], out var ipAddress, out int port))
        {
            ShowUsage();
            return;
        }

        var clientsInfo = new ClientsInfo();
        if (ipAddress == null)
        {
            ShowUsage();
            return;
        }

        TcpListener ServerSocket = new TcpListener(ipAddress, port);
        ServerSocket.Start();

        Console.WriteLine($"Chat server started at {args[0]}.");
        Console.WriteLine($"For exit press Ctrl+C.");
        
        while (true)
        { 
            TcpClient client = ServerSocket.AcceptTcpClient();
            var newClientInfoGuid = Guid.NewGuid();
            clientsInfo.AddNewClient(newClientInfoGuid, client);

            Console.WriteLine($"Connected clientId: {newClientInfoGuid}.");

            Thread t = new Thread(handle_clients);
            t.Start(new CurrentClientInfo(clientsInfo, newClientInfoGuid));
        }
    }

    private static string ReadLine(NetworkStream stream)
    {
        stream.ReadTimeout = 100;
        List<byte> bytes = new List<byte>();
        try
        {
            int curByte = -1;
            while (stream.DataAvailable)
            {
                curByte = stream.ReadByte();
                if (curByte == -1)
                    break;
                if (curByte == '\r')
                    break;
                bytes.Add( (byte) curByte);
            }
        }
        catch (IOException)
        {
            return string.Empty;
        }
        if (bytes.Count == 0)
        {
            return string.Empty;
        }
        return Encoding.ASCII.GetString(bytes.ToArray());
    }

    private static bool CheckIsAlive(NetworkStream stream)
    {
        const string isAliveCmd = "IsAlive\r";
        const string isAliveResponse = "OK";
        stream.WriteTimeout = 500;
        byte[] buffer = Encoding.ASCII.GetBytes(isAliveCmd);
        stream.Write(buffer,0, buffer.Length);
        string response = ReadLine(stream);
        return response == isAliveResponse;
    }
    
    private static void ShowUsage()
    {
        Console.WriteLine("Unable parse ip address. Example use: ChatServer 192.168.1.1:5000.");
    }

    static public void handle_clients(object o)
    {
        const ulong NoReponseCountLimit = 100;
        const string healthCheckCmd = "\tHealtchCheck\t";
        CurrentClientInfo currentClientInfo = (CurrentClientInfo) o;
        TcpClient client =  currentClientInfo.AllClients.GetTcpClient(currentClientInfo.guid);
        
        try
        {
            ulong noResponseCount = 0;
            while (true)
            {
                string response = string.Empty;
                try
                {
                    NetworkStream stream = client.GetStream();
                    response = ReadLine(stream);
                }
                catch (Exception)
                {
                    currentClientInfo.AllClients.RemoveKilledClient(currentClientInfo.guid);
                    Console.WriteLine($"Disconnected clientId: {currentClientInfo.guid}.");
                    return;
                }
                Task.Delay(500).Wait();

                if (noResponseCount == NoReponseCountLimit)
                {
                    currentClientInfo.AllClients.RemoveKilledClient(currentClientInfo.guid);
                    Console.WriteLine($"Disconnected clientId: {currentClientInfo.guid}.");
                    return;
                }

                if (response == string.Empty)
                {
                    noResponseCount++;
                    continue;
                }

                noResponseCount = 0;
                if (response == healthCheckCmd)
                {
                    continue;
                }
               
                
                broadcast(currentClientInfo.AllClients, response, currentClientInfo.guid);
                Console.WriteLine(response);
            }
        } 
        catch (IOException e)
        {
            Console.WriteLine(e);
        }
    }

    public static void broadcast(ClientsInfo clientsInfo, string data, Guid senderClientGuid)
    {
        foreach(var item in clientsInfo.GetAllClients(senderClientGuid))
        {
            try
            {
                NetworkStream stream = item.Value.GetStream();
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                stream.Write(buffer,0, buffer.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

}