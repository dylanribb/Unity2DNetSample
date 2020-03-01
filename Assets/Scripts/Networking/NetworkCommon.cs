using UnityEngine;
using Unity.Networking.Transport;

public interface INetworkCallbacks
{
    void OnConnect(int clientId);
    void OnDisconnect(int clientId);
    //void OnEvent(int clientId, NetworkEvent info);

    void OnConnectionAck(int clientId);
    void OnPlayerCommand(PlayerCommand cmd);
}

public interface ISerializableCommand
{
    void SerializeToStream(ref DataStreamWriter writer);
    void DeserializeFromStream(DataStreamReader stream);
}

public static class NetworkConfig
{
    public const int defaultServerPort = 7050;
    public const int maxFragments = 16;
    public const int packageFragmentSize = NetworkParameterConstants.MTU - 128;  // 128 is just a random safety distance to MTU
    public const int maxPackageSize = maxFragments * packageFragmentSize;
    public const int disconnectTimeout = 30000;
}

//public enum NetworkCommandType
//{
//    PlayerConnected = 1 << 0,

//    ConnectionAck = 1 << 1,

//    PlayerCommand = 1 << 2
//}

//public class NetworkCommand : ISerializableCommand
//{
//    public int playerId;

//    public string testMessage;

//    public NetworkCommandType type;
//}
