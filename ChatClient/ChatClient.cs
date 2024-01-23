using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    static object _sendLock = new object();
    static bool _serverRunning = false;

    static (TcpClient client, NetworkStream stream) TryConnectToServer(IPAddress ipAddress, int port)
    {
        try
        {
            var client = new TcpClient();
            client.Connect(ipAddress, port);
            return (client, client.GetStream());
        }
        catch (Exception)
        {
            return (null, null);
        }
    }

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

        var res = TryConnectToServer(ipAddress, port);
        if (res == (null, null))
        {
            Console.WriteLine($"Unable to connect to server {ipAddress}:{port}.");
            return;
        }

        Console.WriteLine("client connected!!");
        _serverRunning = true;

        Timer timer = new Timer(HealthCheckTimer, res.stream, 0, 5000);        
        
        Thread receiveThread = new Thread(receiveListener);
        receiveThread.Start(res.stream);
       
        try
        {
            string s;
            while (_serverRunning)
            {
                s = Console.ReadLine();
                s+= '\r';
                if (!SendMessage(s, res.stream))
                {
                    _serverRunning = false;
                }
            }
        }
        finally
        {
            res.stream.Close();
            res.client.Close();
            Console.WriteLine("disconnect from server!!");
        }
    }

    private static bool SendMessage(string text, NetworkStream stream)
    {
        try
        {
            lock (_sendLock)
            {
                byte[] buffer = Encoding.ASCII.GetBytes(text);
                stream.Write(buffer, 0, buffer.Length);
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void HealthCheckTimer(Object o) 
    {
        const string healthCheckCmd = "\tHealtchCheck\t";
        NetworkStream ns = (NetworkStream) o;

        if (!SendMessage(healthCheckCmd+'\r', ns))
            _serverRunning = false;
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

    
    public static void receiveListener(object o)
    {
        NetworkStream ns = (NetworkStream)o;
        ns.ReadTimeout = 100;
        while (_serverRunning)
        {
            string response = ReadLine(ns);
            if (response == string.Empty)
            {
                continue;
            }
            Console.WriteLine(response);
        }
    }

    private static void ShowUsage()
    {
        Console.WriteLine("Unable parse ip address. Example use: ChatClient 192.168.1.1:5000.");
    }

}
