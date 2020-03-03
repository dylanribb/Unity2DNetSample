using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;

public class NetworkServer
{
    private Dictionary<int, NetworkConnection> connections = new Dictionary<int, NetworkConnection>();
    private NetworkDriver driver;
    private ServerInfo serverInfo;

    public class ServerInfo
    {
        public float serverTickRate;
    }

    public int serverTime { get; private set; }

    public NetworkServer(NetworkDriver driver, float serverTickRate)
    {
        this.driver = driver;
        this.serverInfo = new ServerInfo
        {
            serverTickRate = serverTickRate
        };
    }

    public void Init()
    {
        NetworkEndPoint endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = NetworkConfig.defaultServerPort;

        if (this.driver.Bind(endpoint) != 0)
        {
            Debug.Log($"Failed to bind to port {NetworkConfig.defaultServerPort}");
            return;
        }

        driver.Listen();
        Debug.Log($"Server Listening on Port {NetworkConfig.defaultServerPort}");
    }

    public void Update(INetworkCallbacks loop)
    {
        this.driver.ScheduleUpdate().Complete();

        this.CleanUpConnections(loop);

        this.AcceptNewConnections(loop);

        this.ReadDataStream(loop);

        //this.SendData();

    }

    public void CleanUpConnections(INetworkCallbacks loop)
    {
        List<int> connectionsToRemove = new List<int>();

        foreach (NetworkConnection connection in this.connections.Values)
        {
            if (!connection.IsCreated)
            {
                Debug.Log($"Removing Connection {connection.InternalId}");
                connectionsToRemove.Add(connection.InternalId);
            }
        }

        if (connectionsToRemove.Count == 0) { return; }

        foreach (int connectionId in connectionsToRemove)
        {
            this.connections.Remove(connectionId);
            this.OnDisconnect(connectionId, loop);
        }
    }

    public void AcceptNewConnections(INetworkCallbacks loop)
    {
        NetworkConnection conn;
        while ((conn = this.driver.Accept()) != default(NetworkConnection))
        {
            this.connections[conn.InternalId] = conn;
            Debug.Log($"Accepted a connection. Connection ID: {conn.InternalId}");
            loop.OnConnect(conn.InternalId);
        }
    }

    public void SendPlayerConnectionAck(int playerId)
    {
        Debug.Log($"(Server) Sending Connection ACK. PlayerId: {playerId}");

        NetworkConnection conn;
        this.connections.TryGetValue(playerId, out conn);

        if (conn == null)
        {
            Debug.LogError($"Could not acknowledge connection for Player: {playerId}");
            return;
        }

        PlayerCommand ackCmd = new PlayerCommand().OfType(PlayerCommandType.ConnectionAck).WithPlayerId(playerId);
        ackCmd.serverTickRate = this.serverInfo.serverTickRate;

        DataStreamWriter writer = this.driver.BeginSend(conn);
        ackCmd.SerializeToStream(ref writer);
        this.driver.EndSend(writer);
    }

    public void NotifyPlayersOfNewConnection(int playerId)
    {
        PlayerCommand cmd = new PlayerCommand().OfType(PlayerCommandType.PlayerConnected).WithPlayerId(playerId);
        Debug.Log("Command Type: " + cmd.Type.ToString());
    

        foreach (var pair in this.connections)
        {
            if (pair.Value.InternalId == playerId) { continue; }

            DataStreamWriter writer = this.driver.BeginSend(pair.Value);
            cmd.SerializeToStream(ref writer);
            this.driver.EndSend(writer);
        }   
    }

    public void SendPlayerSnapshots(Queue<PlayerCommand> snapshots)
    {
        while (snapshots.Count > 0)
        {
            PlayerCommand cmd = snapshots.Dequeue();

            foreach (var pair in this.connections)
            {
                DataStreamWriter writer = this.driver.BeginSend(pair.Value);
                cmd.SerializeToStream(ref writer);
                this.driver.EndSend(writer);
            }
        }
     
    }

    public void ReadDataStream(INetworkCallbacks loop)
    {

        List<int> disconnectedIds = new List<int>();
        DataStreamReader stream;
        foreach (var pair in this.connections)
        {

            NetworkEvent.Type evtType;
            PlayerCommand cmd;
            while ((evtType = this.driver.PopEventForConnection(pair.Value, out stream)) != NetworkEvent.Type.Empty)
            {
                if (evtType == NetworkEvent.Type.Data)
                {
                    Debug.Log($"(Server) Stream Length: {stream.Length}");
                    Debug.Log("Stream Created (Server)?: " + stream.IsCreated);

                    cmd = new PlayerCommand();
                    cmd.DeserializeFromStream(stream);
                    loop.OnPlayerCommand(cmd);                  
                }
                else if (evtType == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    disconnectedIds.Add(pair.Value.InternalId);
                }
            }
        }

        if (disconnectedIds.Count == 0) { return; }
        foreach (int id in disconnectedIds)
        {
            this.connections[id] = default;
            this.OnDisconnect(id, loop);
        }
    }

    private void OnConnect(int connectionId, INetworkCallbacks loop)
    {


        if (this.connections.Count >= ServerGameLoop.serverMaxClients)
        {
            Debug.Log("Refusing incoming connection " + connectionId + " due to server.maxclients");
            this.connections[connectionId].Disconnect(this.driver);
            return;
        }

        Debug.Log($"Incoming connection: #{connectionId}");

        loop.OnConnect(connectionId);
    }

    private void OnDisconnect(int connectionId, INetworkCallbacks loop)
    {
        NetworkConnection connection;
        if (this.connections.TryGetValue(connectionId, out connection))
        {

            Debug.Log(string.Format("Client {0} disconnected", connectionId));

            this.connections.Remove(connectionId);

            loop.OnDisconnect(connectionId);
        }
    }
}
