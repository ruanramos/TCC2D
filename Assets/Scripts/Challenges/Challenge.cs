using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using static GameConstants;

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
        private GameObject _challengeTimeout;
        private GameObject _challengeInfo;

        private double _challengeStartTime;

        private void Awake()
        {
            _challengeOuterCanvas = transform.GetChild(0).gameObject;
            _challengeInnerCanvas = transform.GetChild(1).gameObject;
            _challengeTimeout = _challengeOuterCanvas.transform.GetChild(2).gameObject;
            _challengeInfo = _challengeOuterCanvas.transform.GetChild(3).gameObject;
            _challengeHeader = _challengeOuterCanvas.GetComponentInChildren<TextMeshProUGUI>();
            _challengeOuterCanvas.SetActive(false);
            _challengeInnerCanvas.SetActive(false);
            _challengeTimeout.SetActive(false);
            _challengeInfo.SetActive(false);
        }

        private void Start()
        {
            _challengeStartTime = NetworkManager.Singleton.ServerTime.Time;
            _challengeHeader.text = Client1Id.Value == NetworkManager.LocalClient.ClientId
                ? $"Player {Client1Id.Value} X Player {Client2Id.Value}"
                : $"Player {Client2Id.Value} X Player {Client1Id.Value}";

            if (LocalClientInChallenge())
            {
                _challengeOuterCanvas.SetActive(true);
                _challengeInnerCanvas.SetActive(true);
                _challengeTimeout.SetActive(true);
                _challengeInfo.SetActive(true);
            }
        }

        private bool LocalClientInChallenge()
        {
            return (Client1Id.Value == NetworkManager.LocalClient.ClientId ||
                    Client2Id.Value == NetworkManager.LocalClient.ClientId);
        }

        private bool IsInDelayTime()
        {
            return ChallengeDuration() < ChallengeStartDelayInSeconds;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) && LocalClientInChallenge() && CanSendInput())
            {
                SendKeyboardTimestampToServerServerRpc(NetworkManager.Singleton.ServerTime.Time,
                    Input.inputString);
            }

            _challengeTimeout.GetComponent<TextMeshProUGUI>().text = GetTimeoutText();

            if (IsInDelayTime())
            {
                _challengeInfo.GetComponent<TextMeshProUGUI>().text = GetDelayText();
            }

            // Check if client running this is involved in challenge
            if ((Client1Id.Value == 0 && Client2Id.Value == 0) ||
                (Client1Id.Value != NetworkManager.LocalClient.ClientId &&
                 Client2Id.Value != NetworkManager.LocalClient.ClientId) ||
                _challengeHeader.text.Equals($"Player {Client1Id.Value} X Player {Client2Id.Value}") ||
                _challengeHeader.text.Equals($"Player {Client2Id.Value} X Player {Client1Id.Value}")) return;

            // If client is involved in challenge, show canvas
            _challengeOuterCanvas.SetActive(true);
            _challengeInnerCanvas.SetActive(true);
            _challengeTimeout.SetActive(true);
            _challengeInfo.SetActive(true);

            // Make local client name appear first in challenge header
            _challengeHeader.text = Client1Id.Value == NetworkManager.LocalClient.ClientId
                ? $"Player {Client1Id.Value} X Player {Client2Id.Value}"
                : $"Player {Client2Id.Value} X Player {Client1Id.Value}";
        }

        private string GetTimeoutText()
        {
            var timeoutCounter = ChallengeTimeoutLimitInSeconds - ChallengeDuration() + ChallengeStartDelayInSeconds;

            var timeoutText = timeoutCounter > ChallengeTimeoutLimitInSeconds ? ChallengeTimeoutLimitInSeconds :
                timeoutCounter < 0 ? 0 : Math.Round(timeoutCounter, 2);

            return $"{timeoutText}";
        }

        private double ChallengeDuration()
        {
            return NetworkManager.Singleton.ServerTime.Time - _challengeStartTime;
        }

        private bool CanSendInput()
        {
            return !IsInDelayTime();
        }

        private string GetDelayText()
        {
            var delayCounter = ChallengeStartDelayInSeconds - ChallengeDuration();

            var delayText = delayCounter > ChallengeStartDelayInSeconds ? ChallengeStartDelayInSeconds :
                delayCounter < 0 ? 0 : Math.Round(delayCounter, 2);

            return delayText == 0 ? "GO!" : $"Starts in {delayText}s";
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            Destroy(gameObject);
        }

        public ulong DecideWinner()
        {
            switch (_clientFinishTimestamps.Count)
            {
                // Check if any player pressed space
                case 0:
                    print(
                        $"<color=#FF00AA>Challenge between {Client1Id.Value} and {Client2Id.Value} had no winner</color>");
                    return 0;

                // Check if both players pressed space
                case < 2:
                {
                    // Print what player didn't finish the challenge
                    foreach (var clientId in new[] { Client1Id.Value, Client2Id.Value })
                    {
                        if (!_clientFinishTimestamps.ContainsKey(clientId))
                        {
                            print(
                                $"<color=#FF00AA>{clientId} didn't finish the challenge</color>");
                            // Set timestamp to ulong.MaxValue if client didn't finish challenge
                            _clientFinishTimestamps[clientId] = ulong.MaxValue;
                        }
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

        [Rpc(SendTo.Server)]
        public void SetWinnerTextServerRpc(ulong client1Id, ulong client2Id, ulong winnerId,
            RpcParams rpcParams = default)
        {
            SetWinnerTextClientRpc(winnerId, RpcTarget.Group(new[] { client1Id, client2Id }, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void SetWinnerTextClientRpc(ulong winnerId, RpcParams rpcParams = default)
        {
            print(
                $"<color=#FF00AA>Setting winner text for challenge between {Client1Id.Value} and {Client2Id.Value}</color>");
            _challengeInfo.GetComponent<TextMeshProUGUI>().text = winnerId == 0
                ? "No winner"
                : $"Player {winnerId} wins";
        }
    }
}