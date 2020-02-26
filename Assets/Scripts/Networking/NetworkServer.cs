using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using MsgPack.Serialization;

public class NetworkServer
{
    private Dictionary<int, NetworkConnection> connections = new Dictionary<int, NetworkConnection>();
    private NetworkDriver driver;
    private ServerInfo serverInfo;

    public class ServerInfo
    {
        public int serverTickRate;
    }

    public int serverTime { get; private set; }

    public NetworkServer(NetworkDriver driver)
    {
        this.driver = driver;
        this.serverInfo = new ServerInfo();
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
        NetworkConnection conn;
        this.connections.TryGetValue(playerId, out conn);

        if (conn == null)
        {
            Debug.LogError($"Could not acknowledge connection for Player: {playerId}");
            return;
        }

        NetworkCommand ackCmd = default;
        ackCmd.type = NetworkCommandType.ConnectionAck;
        ackCmd.playerId = playerId;

        MessagePackSerializer<NetworkCommand> serializer = MessagePackSerializer.Get<NetworkCommand>();
        MemoryStream strm = new MemoryStream();
        serializer.Pack(strm, ackCmd);

        using (NativeArray<byte> streamBytes = new NativeArray<byte>(strm.GetBuffer(), Allocator.Temp))
        {
            DataStreamWriter writer = this.driver.BeginSend(conn);
            writer.WriteBytes(streamBytes);
            this.driver.EndSend(writer);
        }
          

    }

    public void NotifyPlayersOfNewConnection(int playerId)
    {
        NetworkCommand cmd = new NetworkCommand();
        cmd.type = NetworkCommandType.PlayerConnected;
        Debug.Log("Command Type: " + cmd.type.ToString());
        cmd.playerId = playerId;

        MessagePackSerializer<NetworkCommand> serializer = MessagePackSerializer.Get<NetworkCommand>();
        MemoryStream strm = new MemoryStream();
        serializer.Pack(strm, cmd);
        using (NativeArray<byte> streamBytes = new NativeArray<byte>(strm.GetBuffer(), Allocator.Temp))
        {

            foreach (var pair in this.connections)
            {
                if (pair.Value.InternalId == playerId) { continue; }

                DataStreamWriter writer = this.driver.BeginSend(pair.Value);
                writer.WriteBytes(streamBytes);
                this.driver.EndSend(writer);
            }
        }
            
    }

    public void ReadDataStream(INetworkCallbacks loop)
    {
        MessagePackSerializer<PlayerCommand> serializer = MessagePackSerializer.Get<PlayerCommand>();

        List<int> disconnectedIds = new List<int>();
        DataStreamReader stream;
        foreach (var pair in this.connections)
        {

            NetworkEvent.Type evtType;
            MemoryStream memStream;
            PlayerCommand cmd;
            while ((evtType = this.driver.PopEventForConnection(pair.Value, out stream)) != NetworkEvent.Type.Empty)
            {
                if (evtType == NetworkEvent.Type.Data)
                {
                    Debug.Log($"(Server) Stream Length: {stream.Length}");
                    Debug.Log("Stream Created (Server)?: " + stream.IsCreated);


                    byte[] streamBytes = new byte[stream.Length];

                    for (int i = 0; i <stream.Length; i++)
                    {
                         
                        streamBytes[i] = stream.ReadByte();
                    }
                    
                    Debug.Log($"(Server) Stream Byte Length: {streamBytes.Length}");
                    memStream = new MemoryStream();
                    memStream.Write(streamBytes, 0, streamBytes.Length);
                    memStream.Position = 0;

                    Debug.Log($"(Server) Mem Stream Length: {memStream.Length}");
                    cmd = serializer.Unpack(memStream);

                    Debug.Log($"(Server) Test Message (Command Type): {cmd.Type}");

                    Debug.Log("(Server) Bytes Read: " + stream.GetBytesRead());

                  
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
