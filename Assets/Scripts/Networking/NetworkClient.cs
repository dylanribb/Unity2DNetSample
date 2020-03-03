using System.IO;
using System.Net;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;

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
        PlayerCommand cmd;

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

                    Debug.Log($"(Server) Stream Length: {stream.Length}");

                    cmd = new PlayerCommand();
                    cmd.DeserializeFromStream(stream);

                    if ((cmd.Type & PlayerCommandType.PlayerConnected) != 0)
                    {
                        loop.OnConnect(cmd.PlayerID);
                    }

                    if ((cmd.Type & PlayerCommandType.ConnectionAck) != 0)
                    {

                        int playerId = cmd.PlayerID;
                        Debug.Log($"(Client) Received Connection ACK. PlayerId: {playerId}");
                        loop.OnConnectionAck(cmd);
                    }

                    if ((cmd.Type & PlayerCommandType.Snapshot) != 0)
                    {
                        Debug.Log($"Received Snapshots");
                        loop.OnReceiveSnapshot(cmd);
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

    public void SendQueuedCommands(ref Queue<PlayerCommand> playerCommands)
    {
        Debug.Log("Sending Queued Commands...");

        if (playerCommands.Count <= 0) {
            Debug.Log(" (Client) No Messages To Send");
            return;
        }

        while (playerCommands.Count > 0)
        {
            ISerializableCommand cmd = playerCommands.Dequeue();
            DataStreamWriter writer = this.driver.BeginSend(this.connection);
            cmd.SerializeToStream(ref writer);
            this.driver.EndSend(writer);
        }
    }

    private NetworkDriver driver;
    private NetworkConnection connection;
}
