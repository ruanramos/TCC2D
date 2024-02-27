using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Challenges
{
    public class Challenge : NetworkBehaviour, IEquatable<Challenge>
    {
        public NetworkVariable<ulong> Client1Id { get; set; } = new();
        public NetworkVariable<ulong> Client2Id { get; set; } = new();

        private TextMeshProUGUI _challengeHeader;
        private GameObject _challengeOuterCanvas;
        private GameObject _challengeInnerCanvas;

        private void Awake()
        {
            print(
                $"<color=#FFFF00>Challenge awake at time {NetworkManager.Singleton.ServerTime.Time}</color>");
            _challengeOuterCanvas = transform.GetChild(0).gameObject;
            _challengeInnerCanvas = transform.GetChild(1).gameObject;
            _challengeHeader = _challengeOuterCanvas.GetComponentInChildren<TextMeshProUGUI>();
            _challengeOuterCanvas.SetActive(false);
            _challengeInnerCanvas.SetActive(false);
        }

        private void Start()
        {
            print(
                $"<color=#FFFF00>Challenge start at time {NetworkManager.Singleton.ServerTime.Time}</color>");
            print($"Client1Id: {Client1Id.Value} Client2Id: {Client2Id.Value}");
            _challengeHeader.text = $"Player {Client1Id.Value} X Player {Client2Id.Value}";
            print(
                $"<color=#FF0000>C1: {Client1Id.Value} - C2: {Client2Id.Value} - Owner: {OwnerClientId} - Localclient: {NetworkManager.LocalClient.ClientId}</color>");
            if ((Client1Id.Value == NetworkManager.LocalClient.ClientId ||
                 Client2Id.Value == NetworkManager.LocalClient.ClientId))
            {
                _challengeOuterCanvas.SetActive(true);
                _challengeInnerCanvas.SetActive(true);
            }
        }

        private void Update()
        {
            // Check if client running this is involved in challenge
            if ((Client1Id.Value == 0 && Client2Id.Value == 0) ||
                (Client1Id.Value != NetworkManager.LocalClient.ClientId &&
                 Client2Id.Value != NetworkManager.LocalClient.ClientId) ||
                _challengeHeader.text.Equals($"Player {Client1Id.Value} X Player {Client2Id.Value}")) return;
            _challengeOuterCanvas.SetActive(true);
            _challengeHeader.text = $"Player {Client1Id.Value} X Player {Client2Id.Value}";
        }

        public override void OnNetworkSpawn()
        {
            print("Challenge spawned");
            Client1Id.OnValueChanged += TreatClient1IdChanged;
            Client2Id.OnValueChanged += TreatClient2IdChanged;
        }

        private void TreatClient1IdChanged(ulong previousId, ulong currentId)
        {
            print(
                $"<color=#FFFF00>Changed client 1 id from {previousId} to {currentId} at time {NetworkManager.Singleton.ServerTime.Time}</color>");
        }

        private void TreatClient2IdChanged(ulong previousId, ulong currentId)
        {
            print(
                $"<color=#FFFF00>Changed client 2 id from {previousId} to {currentId} at time {NetworkManager.Singleton.ServerTime.Time}</color>");
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            Client1Id.OnValueChanged -= TreatClient1IdChanged;
            Client2Id.OnValueChanged -= TreatClient2IdChanged;
            Destroy(gameObject);
        }

        public bool Equals(Challenge other)
        {
            return (Client1Id.Value == other.Client1Id.Value && Client2Id.Value == other.Client2Id.Value ||
                    Client1Id.Value == other.Client2Id.Value && Client2Id.Value == other.Client1Id.Value);
        }

        public override string ToString()
        {
            var result = $"{Client1Id.Value} x {Client2Id.Value}: ";
            return result;
        }
    }
}