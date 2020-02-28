using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public class ServerGameLoop : IGameLoop, INetworkCallbacks
{
    public static int serverMaxClients = 16;

    public static int serverPort;

    private GameObject playerPrefab;

    private NetworkDriver driver;

    public class ClientInfo
    {
        public int id;
        public bool isReady;

    }

    public IGameLoop WithPlayerPrefab(GameObject playerPrefab)
    {
        this.playerPrefab = playerPrefab;
        return this;
    }

    public bool Init(string[] args)
    {
        this.stateMachine = new StateMachine<ServerState>();
        this.stateMachine.Add(ServerState.Idle, null, UpdateIdleState, null);
        // this.stateMachine.Add(ServerState.Loading, null, UpdateLoadingState, null);
        // this.stateMachine.Add(ServerState.Active, EnterActiveState, UpdateActiveState, LeaveActiveState);
        //this.networkTransport = new SocketTransport(NetworkConfig.defaultServerPort, serverMaxClients);
        this.driver = NetworkDriver.Create(new INetworkParameter[0]);
        this.networkServer = new NetworkServer(this.driver);

        Debug.Log("Server Initialized...");

        this.serverStartTime = Time.time;

        this.networkServer.Init();

        return true;
    }

    public void SendTest() { }

    public void Update()
    {
        while (this.playerCommands.Count > 0)
        {
            PlayerCommand cmd = this.playerCommands.Dequeue();

            GameObject player;
            this.players.TryGetValue(cmd.PlayerID, out player);

            player.GetComponent<NetworkPlayer>().QueueCommand(cmd);
        }
        this.networkServer.Update(this);
    }

    public void ShutDown()
    {
        // this.networkTransport.Shutdown();
    }

    public void OnConnect(int id)
    {
        var client = new ClientInfo();
        client.id = id;
        this.clients.Add(id, client);
        Debug.Log($"Added Client With ID: {id}");

        this.InitPlayerObject(id);

        this.networkServer.SendPlayerConnectionAck(id);
        this.networkServer.NotifyPlayersOfNewConnection(id);
    }

    public void OnDisconnect(int id)
    {
        this.clients.Remove(id);
    }

    public void OnPlayerCommand(PlayerCommand command)
    {
        Debug.Log("(Server) Received Player Command");
        this.playerCommands.Enqueue(command);
    }

    public void OnConnectionAck(int playerId) { }

    /// <summary>
    /// Idle state, no level is loaded
    /// </summary>
    private void UpdateIdleState()
    {

    }

    /// <summary>
    /// Loading state, load in progress
    /// </summary>
    private void UpdateLoadingState()
    {
        this.stateMachine.SwitchTo(ServerState.Active);
    }

    // private SocketTransport networkTransport;
    private NetworkServer networkServer;
    private float serverStartTime;
    private Dictionary<int, ClientInfo> clients = new Dictionary<int, ClientInfo>();
    private Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();

    private Queue<PlayerCommand> playerCommands = new Queue<PlayerCommand>();

    enum ServerState
    {
        Idle,
        Loading,
        Active,
    }

    private void InitPlayerObject(int playerId)
    {
        GameObject player = Object.Instantiate(this.playerPrefab);
        players.Add(playerId, player);
    }

    private StateMachine<ServerState> stateMachine;

}
