using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Challenges
{
    public class Challenge : NetworkBehaviour, IEquatable<Challenge>, INetworkSerializable
    {
        public ulong Client1Id { get; set; }
        public ulong Client2Id { get; set; }

        private TextMeshProUGUI _challengeHeader;

        public Challenge(ulong client1Id, ulong client2Id)
        {
            Client1Id = client1Id;
            Client2Id = client2Id;
        }

        public Challenge()
        {
            Client1Id = 0;
            Client2Id = 0;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) && IsOwner)
            {
                SendKeyboardTimestampToServerServerRpc(NetworkManager.Singleton.NetworkTimeSystem.ServerTime,
                    Input.inputString);
            }
        }

        [ServerRpc]
        private void SendKeyboardTimestampToServerServerRpc(double time, string key)
        {
            print(
                $"Received {key} press from client {OwnerClientId}\n" +
                $"timestamp: {time}  --- Server time: {NetworkManager.Singleton.NetworkTimeSystem.ServerTime}");
            print($"Will change _challengeData to add timestamp for client {OwnerClientId}");
        }

        public override void OnNetworkSpawn()
        {
            // Check if owner is involved in challenge
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
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

        public override string ToString()
        {
            var result = $"{Client1Id} x {Client2Id}: ";
            return result;
        }
    }
}