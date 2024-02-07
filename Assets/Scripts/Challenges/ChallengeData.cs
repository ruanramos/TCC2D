using System;
using Unity.Netcode;

namespace Challenges
{
    public class ChallengeData : IEquatable<ChallengeData>, INetworkSerializable
    {
        public ulong Client1Id { get; private set; }
        public ulong Client2Id { get; private set; }

        public ChallengeData(ulong client1Id, ulong client2Id)
        {
            Client1Id = client1Id;
            Client2Id = client2Id;
        }

        public bool Equals(ChallengeData other)
        {
            return (Client1Id == other.Client1Id && Client2Id == other.Client2Id ||
                    Client1Id == other.Client2Id && Client2Id == other.Client1Id);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            var client1Id = Client1Id;
            var client2Id = Client2Id;
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out client1Id);
                reader.ReadValueSafe(out client2Id);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(client1Id);
                writer.WriteValueSafe(client2Id);
            }
        }
    }
}