using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClient
{
    public bool localPlayer = false;

    public int playerId;

    public Queue<PlayerCommand> QueuedCommands
    {
        get
        {
            return this.commandQueue;
        }
    }

    public bool IsLocal
    {
        get
        {
            return this.localPlayer;
        }
    }

    public PlayerClient AsLocalPlayer()
    {
        this.localPlayer = true;
        return this;
    }

    public void EnqueueCommand(PlayerCommand command)
    {
        this.commandQueue.Enqueue(command);
    }

    private Queue<PlayerCommand> commandQueue = default;

}