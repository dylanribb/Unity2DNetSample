public enum PlayerCommandType
{
    Move = 1 << 0,
    Shoot = 1 << 1
}

public class PlayerCommand
{
    private PlayerCommandType type;

    public PlayerCommand(PlayerCommandType type)
    {
        this.type = type;
    }
}