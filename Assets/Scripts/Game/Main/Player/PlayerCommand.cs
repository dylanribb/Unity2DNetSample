using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;

public enum PlayerCommandType
{
    PlayerConnected = 1 << 0,
    PlayerDisconnected = 1 << 1,
    ConnectionAck = 1 << 2,
    Move = 1 << 3,
    Shoot = 1 << 4,
    Snapshot = 1 << 5
}

//[MessagePackKnownType("PlayerMoveCommand", typeof(PlayerMoveCommand))]
public class PlayerCommand : ISerializableCommand
{

    public Vector3 startingPosition;
    public Vector3 endingPosition;
    public Vector3 currentPosition;

    public float serverTickRate;

    public List<int> currentPlayers = new List<int>();

    public PlayerCommand()
    {
        this.currentTick = 0;
    }

    public PlayerCommand(int currentTick)
    {
        this.currentTick = currentTick;
    }

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

    public PlayerCommand WithSequenceNumber(int sequenceNumber)
    {
        this.sequenceNumber = sequenceNumber;
        return this;
    }

    public PlayerCommandType Type
    {
        get { return this.type;  }
    }

    public int PlayerID
    {
        get { return this.playerId; }
    }

    public virtual void SerializeToStream(ref DataStreamWriter writer)
    {
        writer.WriteInt(this.currentTick);
        writer.WriteInt((int)this.type);
        writer.WriteInt(this.playerId);


        if ((type & PlayerCommandType.Move) != 0)
        {
            writer.WritePackedFloat(this.startingPosition.x, this.compressionModel);
            writer.WritePackedFloat(this.startingPosition.y, this.compressionModel);
            writer.WritePackedFloat(this.endingPosition.x, this.compressionModel);
            writer.WritePackedFloat(this.endingPosition.y, this.compressionModel);
        }

        if ((type & PlayerCommandType.ConnectionAck) != 0)
        {
            writer.WritePackedFloat(this.serverTickRate, this.compressionModel);

            BinaryFormatter binFormatter = new BinaryFormatter();
            MemoryStream memStream = new MemoryStream();
            binFormatter.Serialize(memStream, this.currentPlayers);

            NativeArray<byte> playerArray = new NativeArray<byte>(memStream.ToArray(), Allocator.Temp);

            writer.WriteBytes(playerArray);
        }

        if((type & PlayerCommandType.Snapshot) != 0)
        {
            writer.WritePackedFloat(this.currentPosition.x, this.compressionModel);
            writer.WritePackedFloat(this.currentPosition.y, this.compressionModel);
        }
    }

    public virtual void DeserializeFromStream(DataStreamReader stream)
    {
        this.currentTick = stream.ReadInt();
        this.type = (PlayerCommandType)stream.ReadInt();
        this.playerId = stream.ReadInt();

        if ((type & PlayerCommandType.Move) != 0)
        {
            this.startingPosition = new Vector3(stream.ReadPackedFloat(this.compressionModel), stream.ReadPackedFloat(this.compressionModel), 0);
            this.endingPosition = new Vector3(stream.ReadPackedFloat(this.compressionModel), stream.ReadPackedFloat(this.compressionModel), 0);
        }

        if ((type & PlayerCommandType.ConnectionAck) != 0)
        {
            stream.ReadPackedFloat(this.compressionModel);

            int bytesRead = stream.GetBytesRead();

            byte[] playerByteArray = new byte[stream.Length - bytesRead];
            for (int i = 0; i < stream.Length - bytesRead; i++)
            {
                playerByteArray[i] = stream.ReadByte();
            }

            //NativeArray<byte> playerBytes = new NativeArray<byte>();
            //stream.ReadBytes(playerBytes);

            BinaryFormatter binFormatter = new BinaryFormatter();
            MemoryStream memStream = new MemoryStream();
             //= playerBytes.ToArray();
            memStream.Write(playerByteArray, 0, playerByteArray.Length);
            memStream.Position = 0;

            this.currentPlayers = (List<int>)binFormatter.Deserialize(memStream);
        }

        if ((type & PlayerCommandType.Snapshot) != 0)
        {
            this.currentPosition = new Vector3(stream.ReadPackedFloat(this.compressionModel), stream.ReadPackedFloat(this.compressionModel), 0);
        }
    }

    private PlayerCommandType type;
    private int playerId;
    private int currentTick;
    private int sequenceNumber;
    private NetworkCompressionModel compressionModel;
}


//public class PlayerMoveCommand : PlayerCommand
//{
//    public PlayerMoveCommand(Vector3 startingPosition, Vector3 endingPosition)
//    {
//        OfType(PlayerCommandType.Move);
//        this.startingPosition = startingPosition;
//        this.endingPosition = endingPosition;
//        this.compressionModel = new NetworkCompressionModel();
//    }

//    public Vector3 startingPosition;
//    public Vector3 endingPosition;

//    public override void SerializeToStream(ref DataStreamWriter writer)
//    {
//        base.SerializeToStream(ref writer);
//        writer.WritePackedFloat(this.startingPosition.x, this.compressionModel);
//        writer.WritePackedFloat(this.startingPosition.y, this.compressionModel);
//        writer.WritePackedFloat(this.endingPosition.x, this.compressionModel);
//        writer.WritePackedFloat(this.endingPosition.y, this.compressionModel);
//    }

//    public override void DeserializeFromStream(DataStreamReader stream)
//    {
//        base.DeserializeFromStream(stream);
//        stream.ReadPackedFloat(this.compressionModel);
//        stream.ReadPackedFloat(this.compressionModel);
//        stream.ReadPackedFloat(this.compressionModel);
//        stream.ReadPackedFloat(this.compressionModel);
//    }


//}