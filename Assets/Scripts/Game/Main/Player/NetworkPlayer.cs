using UnityEngine;
using System.Collections.Generic;

public class NetworkPlayer : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
       while (this.commands?.Count > 0)
        {
            PlayerCommand cmd = this.commands.Dequeue();
            this.ProcessCommand(cmd);
        }
    }

    private void ProcessCommand(PlayerCommand cmd)
    {
        if (cmd.Type == PlayerCommandType.Move)
        {
            ProcessPlayerMoveCommand(cmd);
        }
    }

    private void ProcessPlayerMoveCommand(PlayerCommand cmd)
    {
        Debug.Log($" (Server) Moving Player. New Position = X: {cmd.endingPosition.x}, Y: {cmd.endingPosition.y}");
        this.transform.position = Vector3.Lerp(cmd.startingPosition, cmd.endingPosition, 100 * Time.deltaTime);
    }

    public void QueueCommand(PlayerCommand cmd)
    {
        if (this.commands is null)
        {
            this.commands = new Queue<PlayerCommand>();
        }

        commands.Enqueue(cmd);
    }

    public PlayerCommand GetCurrentSnapshot(int playerId)
    {
        PlayerCommand cmd = new PlayerCommand()
                                .OfType(PlayerCommandType.Snapshot)
                                .WithPlayerId(playerId);

        cmd.currentPosition = this.transform.position;

        return cmd;
    }

    private Queue<PlayerCommand> commands;
}
