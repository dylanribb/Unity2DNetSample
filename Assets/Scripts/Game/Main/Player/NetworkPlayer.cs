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
            ProcessPlayerMoveCommand((PlayerMoveCommand)cmd);
        }
    }

    private void ProcessPlayerMoveCommand(PlayerMoveCommand cmd)
    {
        Debug.Log($"Moving Player. New Position = X: {cmd.endingPosition.x}, Y: {cmd.endingPosition.y}");
        this.transform.position = cmd.endingPosition;
    }

    public void QueueCommand(PlayerCommand cmd)
    {
        if (this.commands is null)
        {
            this.commands = new Queue<PlayerCommand>();
        }

        commands.Enqueue(cmd);
    }

    private Queue<PlayerCommand> commands;
}
