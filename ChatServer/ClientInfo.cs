using System;
using System.Collections.Generic;
using System.Net.Sockets;

public class ClientsInfo
{
    private Dictionary<Guid, TcpClient> _allClientsInfos;
    private HashSet<Guid> _clientsToKill;
    private object _clientInfoLock;

    public ClientsInfo()
    {
        _allClientsInfos = new Dictionary<Guid, TcpClient>();
        _clientsToKill = new HashSet<Guid>();
        _clientInfoLock = new object();
    }

    public Dictionary<Guid, TcpClient> GetAllClients(Guid? clientIdForSkip = null)
    {
        var res = new Dictionary<Guid, TcpClient>();
        lock (_clientInfoLock)
        {
            foreach (var item in _allClientsInfos)
            {
                if (clientIdForSkip != null && item.Key == clientIdForSkip)
                {
                    continue;
                }
                res.Add(item.Key, item.Value);
            }
        }
        return res;
    }

    public void AddNewClient(Guid guid, TcpClient client)
    {
        lock (_clientInfoLock)
        {
           _allClientsInfos.Add(guid, client);
        }
    }

    public void AddClientToKill(Guid guid)
    {
        lock (_clientInfoLock)
        {
           _clientsToKill.Add(guid);
        }
    }

    public bool IsClientToKill(Guid guid)
    {
        return _clientsToKill.Contains(guid);
    }

    public void RemoveKilledClient(Guid guid)
    {
        lock (_clientInfoLock)
        {
            _allClientsInfos.Remove(guid);
            _clientsToKill.Remove(guid);
        }
    }

    public TcpClient GetTcpClient(Guid guid)
    {
        if (!_allClientsInfos.TryGetValue(guid, out var client))
        {
            return null;
        }
        return client;
    }
    
}