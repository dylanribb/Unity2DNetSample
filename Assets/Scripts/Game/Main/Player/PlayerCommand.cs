using UnityEngine;
using MsgPack.Serialization;

public enum PlayerCommandType
{
    Move = 1 << 0,
    Shoot = 1 << 1
}

public class PlayerCommand
{
    [MessagePackMemberAttribute(0)]
    private PlayerCommandType type;

    [MessagePackMemberAttribute(1)]
    private int playerId;

    public PlayerCommand WithPlayerId(int playerId)
    {
        this.playerId = playerId;
        return this;
    }

    public PlayerCommand OfType(PlayerCommandType type)
    {
        this.type = type;
        return this;
    }

    public PlayerCommandType Type
    {
        get { return this.type;  }
    }
}

public class PlayerMoveCommand : PlayerCommand
{
    public PlayerMoveCommand(Vector3 startingPosition, Vector3 endingPosition) {
        OfType(PlayerCommandType.Move);
        this.startingPosition = startingPosition;
        this.endingPosition = endingPosition;
    }

    private Vector3 startingPosition;
    private Vector3 endingPosition;
}