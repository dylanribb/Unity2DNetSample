using UnityEngine;
using Unity.Networking.Transport;

public interface IGameLoop
{
    bool Init(string[] args);
    void Update();

    void ShutDown();

    void SendTest();

    IGameLoop WithPlayerPrefab(GameObject playerPrefab);

    void OnPlayerCommand(PlayerCommand cmd);

    void OnReceiveSnapshot(PlayerCommand cmd);
}

public class GameTime
{

    public int TickRate
    {
        get { return this.tickRate; }
        set
        {
            this.tickRate = value;
            this.tickInterval = 1.0f / this.tickRate;
        }
    }

    public float tickInterval { get; private set; }
    public int tick;
    public float tickDuration;

    public GameTime(int tickRate)
    {
        this.tickRate = tickRate;
        this.tickInterval = 1.0f / tickRate;
        this.tick = 1;
        this.tickDuration = 0;
    }

    private int tickRate;
}

public class Game : MonoBehaviour
{

    public static double frameTime;
    public static Game game;
    public GameObject playerPrefab;

    public bool RunServer = false;

    public GameObject localPlayerPrefab;


    private ServerGameLoop serverLoop;
    private ClientGameLoop clientLoop;

    public void Awake()
    {
        this.clockFrequency = System.Diagnostics.Stopwatch.Frequency;
        this.clock = new System.Diagnostics.Stopwatch();
        this.clock.Start();

#if UNITY_SERVER
        this.RunServer = true;
#endif
    }

    public void Update()
    {
        frameTime = (double)this.clock.ElapsedTicks / this.clockFrequency;

        if (this.serverLoop == null && this.RunServer)
        {
            this.StartServerGame();
        }

        if (this.serverLoop != null) {
            this.serverLoop.Update();
        }        

        if (this.clientLoop != null)
        {
            this.clientLoop.Update();
        }

    }

    public void OnDestroy()
    {
        if (this.serverLoop != null)
        {
            this.serverLoop.ShutDown();
        }
        

        if (this.clientLoop != null)
        {
            this.clientLoop.ShutDown();
        }

    }

    private void OnApplicationQuit()
    {
        if (this.clientLoop != null)
        {
            this.clientLoop.ShutDown();
        }

        if (this.serverLoop != null)
        {
            this.serverLoop.ShutDown();
        }
    }

    public void StartClientGame()
    {
        if (this.clientLoop == null)
        {
            this.clientLoop = new ClientGameLoop() as ClientGameLoop;
            this.clientLoop.localPlayerPrefab = this.localPlayerPrefab;
            this.clientLoop.networkPlayerPrefab = this.playerPrefab;
            this.clientLoop.Init(null);
        }
        else
        {
            Debug.Log("Client Game Already Started");
        }
    }

    public void SendTestData() {
        if (this.clientLoop != null) {
            this.clientLoop.SendTest();
        }
    }

    public void SendPing() {
        long pingTime = NetworkUtils.PingServer("localhost");
        Debug.Log($"Current Ping: {pingTime}ms");
    } 

    public void StartServerGame()
    {

        this.RunServer = true;

        if (this.RunServer) {
            this.serverLoop = new ServerGameLoop().WithPlayerPrefab(this.playerPrefab) as ServerGameLoop;
            this.serverLoop.Init(null);
        }
    }

    private System.Diagnostics.Stopwatch clock;
    private long clockFrequency;
}