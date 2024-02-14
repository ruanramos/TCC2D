using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Challenges
{
    public class Challenge : NetworkBehaviour, IEquatable<Challenge>, INetworkSerializable
    {
        public ulong Client1Id { get; private set; }
        public ulong Client2Id { get; private set; }

        private TextMeshProUGUI _challengeHeader;

        public Dictionary<ulong, double> ClientFinishTimestamps;

        public Challenge(ulong client1Id, ulong client2Id)
        {
            Client1Id = client1Id;
            Client2Id = client2Id;
            ClientFinishTimestamps = new Dictionary<ulong, double>();
        }

        public Challenge()
        {
            Client1Id = 0;
            Client2Id = 0;
            ClientFinishTimestamps = new Dictionary<ulong, double>();
        }

        private void Awake()
        {
            _challengeHeader = GameObject.Find("ChallengeHeader").GetComponent<TextMeshProUGUI>();
        }

        public override void OnNetworkSpawn()
        {
            _challengeHeader.text = $"{Client1Id} x {Client2Id}";
        }
        
        public override void OnNetworkDespawn()
        {
            Destroy(gameObject);
        }

        public bool Equals(Challenge other)
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

        public void AddClientFinishTimestamp(ulong client, double timestamp)
        {
            ClientFinishTimestamps.Add(client, timestamp);
        }
        
        // Create a toString method that returns the client IDs and their finish times
        public override string ToString()
        {
            var result = $"{Client1Id} x {Client2Id}: ";
            // foreach (var client in ClientFinishTimestamps)
            // {
            //     result += $"Client {client.Key} finished at {client.Value}\n";
            // }
            return result;
        }
    }
}