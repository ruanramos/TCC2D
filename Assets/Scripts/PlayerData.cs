using System;
using Unity.Netcode;
using UnityEngine;

public struct PlayerData : IEquatable<PlayerData>, INetworkSerializable
{
    public ulong ClientId;
    public Color ClientColor;

    public PlayerData(ulong clientId, Color clientColor)
    {
        ClientId = clientId;
        ClientColor = clientColor;
    }

    public bool Equals(PlayerData other)
    {
        return ClientId == other.ClientId;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out ClientId);
            reader.ReadValueSafe(out ClientColor);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(ClientId);
            writer.WriteValueSafe(ClientColor);
        }
    }
}