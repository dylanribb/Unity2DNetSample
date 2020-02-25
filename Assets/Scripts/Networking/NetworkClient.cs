using System.IO;
using System.Net;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using MsgPack.Serialization;

public class NetworkClient
{
    public ClientConfig clientConfig;

    // Sent from client to server when changed
    public class ClientConfig
    {
        public int serverUpdateRate;            // max bytes/sec
        public int serverUpdateInterval;        // requested tick / update
    }

    public NetworkConnection.State ConnectionState
    {
        get
        {
            if (this.connection.Equals(default(NetworkConnection))) { return NetworkConnection.State.Disconnected; }
            Debug.Log($"Connection ID {this.connection.InternalId}");
            return this.connection.GetState(this.driver);
        }
    }

    public NetworkClient(NetworkDriver driver)
    {
        this.driver = driver;
        this.clientConfig = new ClientConfig();
    }

    public bool Connect(string endpoint)
    {

        if (!this.connection.Equals(default(NetworkConnection)))
        {
            Debug.Log("Must be disconnected before reconnecting");
            return false;
        }

        IPAddress ipAddress;
        int port;
        if (!NetworkUtils.EndpointParse(endpoint, out ipAddress, out port, NetworkConfig.defaultServerPort))
        {
            Debug.Log("Invalid endpoint: " + endpoint);
            return false;
        }

        Debug.Log($"IP Address: {ipAddress.ToString()}:{port}");

        NetworkEndPoint endPoint = NetworkEndPoint.Parse(ipAddress.ToString(), (ushort)port);
        this.connection = this.driver.Connect(endPoint);

        // dylanr - We need a graceful way to figure out if we can connect or not
        // Implement a "check connection" method?
        if (this.connection.InternalId == -1)
        {
            Debug.Log("Connect failed");
            return false;
        }

        // Debug.Log($"Connection created from NetworkClient.Connect()? {this.connection.IsCreated}");
        // Debug.Log($"Client Connection State: {this.connection.GetState(this.driver)}");

        return true;
    }

    public void Disconnect()
    {

        if (this.connection.Equals(default(NetworkConnection))) { return; }

        this.driver.Disconnect(this.connection);
        this.connection = default;
    }

    public void Update(INetworkCallbacks loop)
    {
        this.driver.ScheduleUpdate().Complete();

        DataStreamReader stream;
        NetworkEvent.Type eventType;

        // if (!this.connection.Equals(default(NetworkConnection))) {
        //     Debug.Log($"Client Connection State: {this.connection.GetState(this.driver)}");
        // }

        while ((eventType = this.connection.PopEvent(this.driver, out stream)) != NetworkEvent.Type.Empty)
        {
            switch (eventType)
            {
                case NetworkEvent.Type.Connect:
                    Debug.Log("Received Connect");
                    break;
                case NetworkEvent.Type.Data:
                    Debug.Log("Stream Created?: " + stream.IsCreated);
                    if (!stream.IsCreated) { break; }
                    NetworkCommandType typeCode = (NetworkCommandType)stream.ReadInt();
                    Debug.Log($"Command Type: {typeCode.ToString()}");
                    if ((typeCode & NetworkCommandType.PlayerConnected) != 0)
                    {
                        int playerId = stream.ReadInt();
                        loop.OnConnect(playerId);
                    }

                    if ((typeCode & NetworkCommandType.ConnectionAck) != 0)
                    {
                        int playerId = stream.ReadInt();
                        loop.OnConnectionAck(playerId);
                    }
                    break;
                case NetworkEvent.Type.Disconnect:
                    Debug.Log("Received Disconnect");
                    break;
            }
        }
    }

    private void OnConnect(int connectionId)
    {

        Debug.Log($"Handling OnConnect for ID # {this.connection.InternalId}");
        if (!this.connection.Equals(default(NetworkConnection)) && this.connection.InternalId == connectionId)
        {
            Debug.Assert(this.ConnectionState == NetworkConnection.State.Connected);
        }
    }

    public void SendTestData()
    {
        NetworkCommand cmd = new NetworkCommand();
        cmd.playerId = 1;
        cmd.testMessage = "Testing!";

        MessagePackSerializer<NetworkCommand> serializer = MessagePackSerializer.Get<NetworkCommand>();
        MemoryStream strm = new MemoryStream();
        serializer.Pack(strm, cmd);

        if (this.connection.Equals(default)) { return; }

        Debug.Log($"(Client) Stream Length: {strm.Length}");

        using (NativeArray<byte> byteArray = new NativeArray<byte>(strm.GetBuffer(), Allocator.Temp))
        {
            Debug.Log($"(Client) Native Byte Array Length: {byteArray.Length}");
            DataStreamWriter writer = this.driver.BeginSend(this.connection);
            writer.WriteBytes(byteArray);
            this.driver.EndSend(writer);
        }
            
    }

    // private SocketTransport networkTransport;
    private NetworkDriver driver;
    private NetworkConnection connection;
}
