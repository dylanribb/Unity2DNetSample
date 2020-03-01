using UnityEngine;

public interface IGameLoop
{
    bool Init(string[] args);
    void Update();

    void ShutDown();

    void SendTest();

    IGameLoop WithPlayerPrefab(GameObject playerPrefab);

    void OnPlayerCommand(PlayerCommand cmd);
}

public class Game : MonoBehaviour
{

    public static Game game;
    public GameObject playerPrefab;

    public bool RunServer = false;

    public GameObject localPlayer;

    private ServerGameLoop serverLoop;
    private ClientGameLoop clientLoop;

    public void Update()
    {

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
        this.serverLoop.ShutDown();

        if (this.clientLoop != null)
        {
            this.clientLoop.ShutDown();
        }

    }

    public void StartClientGame()
    {
        if (this.clientLoop == null)
        {
            this.clientLoop = new ClientGameLoop() as ClientGameLoop;
            this.clientLoop.localPlayer = this.localPlayer;
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
}