using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public class ClientGameLoop : IGameLoop, INetworkCallbacks
{
    // private SocketTransport networkTransport;
    private NetworkClient networkClient;
    private ClientState clientState;
    private NetworkDriver driver;
    private int connectRetryCount;
    private string gameMessage;
    private string targetServer = "127.0.0.1";

    private float nextSendTime = Time.time + 0.02f;
    //private static int lastCommandId = 0;

    private DateTime attemptedConnectionTime;

    //private PlayerClient localPlayer;
    private Dictionary<int, GameObject> currentPlayers;

    private int localPlayerId;

    private GameTime predictionTime;

    public GameObject localPlayerPrefab;
    public GameObject localPlayer;
    public GameObject networkPlayerPrefab;

    public int movementSpeed = 5;

    public float frameTimeScale = 1.0f;

    public bool Init(string[] args)
    {
        this.localPlayer = (GameObject)UnityEngine.Object.Instantiate(this.localPlayerPrefab);
        Debug.Log("Starting Client Init...");
        //this.localPlayer = new PlayerClient().AsLocalPlayer();
        this.stateMachine = new StateMachine<ClientState>();
        // m_StateMachine.Add(ClientState.Browsing,    EnterBrowsingState,     UpdateBrowsingState,    LeaveBrowsingState);
        this.stateMachine.Add(ClientState.Connecting, EnterConnectingState, UpdateConnectingState, null);
        this.stateMachine.Add(ClientState.Connected, EnterConnectedState, UpdateConnectedState, null);
        // this.networkTransport = new SocketTransport();
        this.driver = NetworkDriver.Create(new INetworkParameter[0]);
        this.networkClient = new NetworkClient(this.driver);

        this.stateMachine.SwitchTo(ClientState.Connecting);

        Debug.Log("Client initialized");

        return true;
    }

    public IGameLoop WithPlayerPrefab(GameObject playerPrefab) {
        return this;
    }

    public void OnConnect(int id)
    {
        Debug.Log("New Player Connected: " + id);
        GameObject player = (GameObject)UnityEngine.Object.Instantiate(this.networkPlayerPrefab);
        this.currentPlayers.Add(id, player);
    }

    public void OnConnectionAck(PlayerCommand cmd)
    {
        this.predictionTime = new GameTime(60);
        
        Debug.Log("Connection Acknowledged. PlayerID: " + cmd.PlayerID);
        this.localPlayerId = cmd.PlayerID;
        if (this.currentPlayers == null)
        {
            this.currentPlayers = new Dictionary<int, GameObject>();
        }

        this.currentPlayers.Add(cmd.PlayerID, this.localPlayer);

        PlayerCommand moveCommand = new PlayerCommand()
                                      .OfType(PlayerCommandType.Move)
                                      .WithPlayerId(this.localPlayerId);

        moveCommand.startingPosition = Vector3.zero;
        moveCommand.endingPosition = this.localPlayer.transform.position;
        this.QueueCommand(moveCommand);

        Debug.Log($"Current Players: {this.currentPlayers}");
    }

    public void OnDisconnect(int id) { }

    public void OnPlayerCommand(PlayerCommand cmd) { }

    public void OnReceiveSnapshot(PlayerCommand cmd)
    {
        if (cmd.PlayerID == this.localPlayerId) { return;  }

        GameObject player;
        this.currentPlayers.TryGetValue(cmd.PlayerID, out player);

        if (player == null) { return; }
        player.transform.position = cmd.currentPosition;
    }

    public void SendTest()
    {
        //this.networkClient.SendTestData();

        PlayerCommand testCommand = new PlayerCommand()
                                        .OfType(PlayerCommandType.Move)
                                        .WithPlayerId(this.localPlayerId);
        //testCommand.startingPosition = Vector3.zero;
        testCommand.endingPosition = Vector3.right;

        Debug.Log($"Sending Test Command. X: {testCommand.endingPosition.x}, Y: {testCommand.endingPosition.y}");
        this.QueueCommand(testCommand);
    }

    public void ShutDown()
    {
        // this.networkTransport.Shutdown();
    }

    public void Update()
    {
        Vector3 startingPosition = this.localPlayer.transform.position;
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0);

        if (move != Vector3.zero)
        {
            this.localPlayer.transform.position += move * this.movementSpeed * Time.deltaTime;
            PlayerCommand moveCommand = new PlayerCommand()
                                        .OfType(PlayerCommandType.Move)
                                        .WithPlayerId(this.localPlayerId);
            moveCommand.startingPosition = startingPosition;
            moveCommand.endingPosition = this.localPlayer.transform.position;
            this.QueueCommand(moveCommand);
        }

        if (Time.time >= this.nextSendTime && this.commandQueue.Count > 0)
        {
            Debug.Log("Sending Queued Commands...");
            this.networkClient.SendQueuedCommands(ref this.commandQueue);
            this.nextSendTime = Time.time + 0.02f;
        }

        this.networkClient.Update(this);
        this.stateMachine.Update();
    }

    private void EnterConnectingState()
    {
        this.attemptedConnectionTime = DateTime.UtcNow;
        this.clientState = ClientState.Connecting;
        connectRetryCount = 0;
    }

    void UpdateConnectingState()
    {
        switch (this.networkClient.ConnectionState)
        {
            case NetworkConnection.State.Connected:
                this.gameMessage = "Client Connected";
                this.stateMachine.SwitchTo(ClientState.Connected);
                break;
            case NetworkConnection.State.Connecting:
                this.gameMessage = "Connecting...";
                if (this.attemptedConnectionTime.AddSeconds(20) < DateTime.UtcNow)
                {
                    Debug.Log("Connection Timed Out after 20s");
                }
                Debug.Log(this.gameMessage);
                break;
            case NetworkConnection.State.Disconnected:
                if (connectRetryCount < 2)
                {
                    connectRetryCount++;
                    this.gameMessage = string.Format("Trying to connect to {0} (attempt #{1})...", this.targetServer, connectRetryCount);
                    Debug.Log(this.gameMessage);
                    this.networkClient.Connect(this.targetServer);
                }
                else
                {
                    this.gameMessage = "Failed to connect to server";
                    Debug.Log(this.gameMessage);
                    this.networkClient.Disconnect();
                }
                break;
        }
    }

    private void EnterConnectedState()
    {
        this.clientState = ClientState.Connected;
        Debug.Log("Entered Connected State");
    }

    private void UpdateConnectedState() { }

    private void CheckConnection()
    {

    }

    private enum ClientState
    {
        Browsing,
        Connecting,
        Connected,
        Loading,
        Playing,
    }
    private StateMachine<ClientState> stateMachine;


    public void QueueCommand(PlayerCommand cmd)
    {
        this.commandQueue.Enqueue(cmd);
    }


    private List<PlayerCommand> commands = new List<PlayerCommand>();
    private Queue<PlayerCommand> commandQueue = new Queue<PlayerCommand>();

}
