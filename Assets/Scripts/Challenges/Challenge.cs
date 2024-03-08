﻿using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Challenges
{
    public class Challenge : NetworkBehaviour, IEquatable<Challenge>
    {
        public NetworkVariable<ulong> Client1Id { get; set; } = new();
        public NetworkVariable<ulong> Client2Id { get; set; } = new();

        private Dictionary<ulong, double> _clientFinishTimestamps = new();

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
            if (Input.GetKeyDown(KeyCode.Space) &&
                (Client1Id.Value == NetworkManager.LocalClient.ClientId ||
                 Client2Id.Value == NetworkManager.LocalClient.ClientId))
            {
                SendKeyboardTimestampToServerServerRpc(NetworkManager.Singleton.ServerTime.Time,
                    Input.inputString);
            }

            // Check if client running this is involved in challenge
            // If so, show challenge canvas
            if ((Client1Id.Value == 0 && Client2Id.Value == 0) ||
                (Client1Id.Value != NetworkManager.LocalClient.ClientId &&
                 Client2Id.Value != NetworkManager.LocalClient.ClientId) ||
                _challengeHeader.text.Equals($"Player {Client1Id.Value} X Player {Client2Id.Value}")) return;

            // If client is involved in challenge, show canvas
            _challengeOuterCanvas.SetActive(true);
            _challengeInnerCanvas.SetActive(true);
            _challengeHeader.text = $"Player {Client1Id.Value} X Player {Client2Id.Value}";
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            /*/ print the timestamps for each client
            foreach (var (key, value) in _clientFinishTimestamps)
            {
                print($"Client {key} finished at {value}");
            }*/

            base.OnNetworkDespawn();
            Destroy(gameObject);
        }

        public ulong DecideWinner()
        {
            switch (_clientFinishTimestamps.Count)
            {
                // Check if any player pressed space
                case 0:
                    print("No player finished the challenge");
                    return 0;

                // Check if both players pressed space
                case < 2:
                {
                    // Print what player didn't finish the challenge
                    foreach (var clientId in new[] { Client1Id.Value, Client2Id.Value })
                    {
                        if (!_clientFinishTimestamps.ContainsKey(clientId))
                        {
                            print($"Client {clientId} didn't finish the challenge");
                        }
                        
                        // Set timestamp to ulong.MaxValue if client didn't finish challenge
                        _clientFinishTimestamps[clientId] = ulong.MaxValue;
                    }

                    break;
                }
            }

            var winner = _clientFinishTimestamps[Client1Id.Value] < _clientFinishTimestamps[Client2Id.Value]
                ? Client1Id.Value
                : Client2Id.Value;
            print(
                $"<color=#FF00AA>Winner of challenge between {Client1Id.Value} and {Client2Id.Value} is {winner}</color>");
            return winner;
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

        [ServerRpc(RequireOwnership = false)]
        private void SendKeyboardTimestampToServerServerRpc(double time, string key,
            ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;

            print(
                $"Received {key} press from client {clientId}\n" +
                $"timestamp: {time}  --- Server time: {NetworkManager.Singleton.ServerTime.Time}");

            // Store timestamp of key press on server if it's not already stored
            _clientFinishTimestamps.TryAdd(clientId, time);
        }
    }
}