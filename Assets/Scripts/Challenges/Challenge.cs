﻿using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static GameConstants;
using Random = UnityEngine.Random;

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
        private TextMeshProUGUI _challengeTimeoutText;
        private GameObject _challengeInfo;
        private TextMeshProUGUI _challengeInfoText;

        public GameObject startIndicator;
        private Image _startIndicatorImage;

        private double _challengeStartTime;

        private Image _challengeFlashImage;

        private double _randomTimeSugar;
        private double _timeUntilWinner;
        private double _timeUntilStart;

        private void Awake()
        {
            _challengeOuterCanvas = transform.GetChild(0).gameObject;
            _challengeInnerCanvas = transform.GetChild(1).gameObject;
            _challengeTimeout = _challengeOuterCanvas.transform.GetChild(2).gameObject;
            _challengeTimeoutText = _challengeTimeout.GetComponent<TextMeshProUGUI>();
            _challengeInfo = _challengeOuterCanvas.transform.GetChild(3).gameObject;
            _challengeInfoText = _challengeInfo.GetComponent<TextMeshProUGUI>();
            _challengeHeader = _challengeOuterCanvas.GetComponentInChildren<TextMeshProUGUI>();
            _challengeFlashImage = _challengeOuterCanvas.GetComponent<Image>();
            _startIndicatorImage = startIndicator.GetComponent<Image>();

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
                _challengeInfo.SetActive(true);
            }

            _timeUntilWinner = ChallengeTimeoutLimitInSeconds + ChallengeStartDelayInSeconds + _randomTimeSugar;
            _timeUntilStart = ChallengeStartDelayInSeconds + _randomTimeSugar;
        }

        private bool LocalClientInChallenge()
        {
            return (Client1Id.Value == NetworkManager.LocalClient.ClientId ||
                    Client2Id.Value == NetworkManager.LocalClient.ClientId);
        }

        private bool IsInDelayTime()
        {
            return ChallengeDuration() <= _timeUntilStart;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) && LocalClientInChallenge())
            {
                if (ChallengeDuration() > _timeUntilStart &&
                    ChallengeDuration() < _timeUntilWinner && IsInChallengeTime())
                {
                    // Send player positive feedback
                    if (!_clientFinishTimestamps.ContainsKey(NetworkManager.Singleton.LocalClientId))
                    {
                        StartCoroutine(TurnScreenGreenAfterPress());
                    }

                    SendKeyboardTimestampToServerServerRpc(NetworkManager.Singleton.ServerTime.Time,
                        Input.inputString);
                }

                if (ChallengeDuration() < ChallengeStartDelayInSeconds)
                {
                    // Send player negative feedback
                    StartCoroutine(FlashScreenRed());
                }
            }

            _challengeTimeoutText.text = GetTimeoutText();

            if (IsInDelayTime() && _startIndicatorImage.color != Color.red)
            {
                _startIndicatorImage.color = Color.red;
            }

            if (IsInChallengeTime() && _startIndicatorImage.color != Color.green)
            {
                _startIndicatorImage.color = Color.green;
                _challengeTimeout.SetActive(true);
            }

            if (!IsInChallengeTime() && !IsInDelayTime() &&
                (_startIndicatorImage.enabled || _challengeTimeout.activeSelf))
            {
                _startIndicatorImage.enabled = false;
                _challengeTimeout.SetActive(false);
            }

            // Check if client running this is involved in challenge
            if ((Client1Id.Value == 0 && Client2Id.Value == 0) || !LocalClientInChallenge() ||
                _challengeHeader.text.Equals($"Player {Client1Id.Value} X Player {Client2Id.Value}") ||
                _challengeHeader.text.Equals($"Player {Client2Id.Value} X Player {Client1Id.Value}")) return;

            // If client is involved in challenge, show canvas
            _challengeOuterCanvas.SetActive(true);
            _challengeInnerCanvas.SetActive(true);
            _challengeInfo.SetActive(true);

            // Make local client name appear first in challenge header
            _challengeHeader.text = Client1Id.Value == NetworkManager.LocalClient.ClientId
                ? $"Player {Client1Id.Value} X Player {Client2Id.Value}"
                : $"Player {Client2Id.Value} X Player {Client1Id.Value}";
        }

        private string GetTimeoutText()
        {
            var timeoutCounter = ChallengeTimeoutLimitInSeconds - ChallengeDuration() + _timeUntilStart;

            var timeoutText = timeoutCounter > ChallengeTimeoutLimitInSeconds ? ChallengeTimeoutLimitInSeconds :
                timeoutCounter < 0 ? 0 : Math.Round(timeoutCounter, 1);

            return $"{timeoutText}";
        }

        private double ChallengeDuration()
        {
            return NetworkManager.Singleton.ServerTime.Time - _challengeStartTime;
        }

        private bool IsInChallengeTime()
        {
            return !IsInDelayTime() && ChallengeDuration() < _timeUntilWinner;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            Destroy(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            CreateRandomSugarServerRpc();
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
                        if (_clientFinishTimestamps.ContainsKey(clientId)) continue;
                        print(
                            $"<color=#FF00AA>{clientId} didn't finish the challenge</color>");
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

            // Send keypress only if it's not stored yet
            if (_clientFinishTimestamps.ContainsKey(clientId))
            {
                // Send negative feedback to player
                return;
            }

            print(
                $"Received {key} press from client {clientId}\n" +
                $"timestamp: {time}  --- Server time: {NetworkManager.Singleton.ServerTime.Time}");

            // Store timestamp of key press on server if it's not already stored
            _clientFinishTimestamps.TryAdd(clientId, time);
        }

        [Rpc(SendTo.Server)]
        public void DisplayResultsServerRpc(ulong client1Id, ulong client2Id, ulong winnerId,
            RpcParams rpcParams = default)
        {
            var client1Timestamp = _clientFinishTimestamps.GetValueOrDefault(client1Id, 0);
            var client2Timestamp = _clientFinishTimestamps.GetValueOrDefault(client2Id, 0);

            DisplayResultsClientRpc(winnerId, client1Timestamp, client2Timestamp,
                RpcTarget.Group(new[] { client1Id, client2Id }, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void DisplayResultsClientRpc(ulong winnerId, double client1Timestamp, double client2Timestamp,
            RpcParams rpcParams = default)
        {
            if (NetworkManager.Singleton.LocalClientId == winnerId)
            {
                TurnScreenGreenAfterWin();
            }
            else
            {
                TurnScreenRedAfterLose();
            }
            
            var client1Reaction = GetClientReactionTime(client1Timestamp);
            var client2Reaction = GetClientReactionTime(client2Timestamp);

            var client1ReactionTimeText = client1Reaction > ChallengeTimeoutLimitInSeconds
                ? "No response"
                : $"{client1Reaction}";

            var client2ReactionTimeText = client2Reaction > ChallengeTimeoutLimitInSeconds
                ? "No response"
                : $"{client2Reaction}";

            print(
                $"<color=#FF00AA>Setting winner text for challenge between {Client1Id.Value} and {Client2Id.Value}</color>");
            _challengeInfoText.text = winnerId == 0
                ? "No winner"
                : $"Player {Client1Id.Value}: {client1ReactionTimeText} \n" +
                  $" Player {Client2Id.Value}: {client2ReactionTimeText} \n\n" +
                  $" Player {winnerId} wins the challenge!";
        }

        private double GetClientReactionTime(double clientTimestamp)
        {
            return clientTimestamp - _challengeStartTime - ChallengeStartDelayInSeconds - _randomTimeSugar;
        }

        // Coroutine to make screen flash red
        private IEnumerator FlashScreenRed()
        {
            var originalColor = _challengeFlashImage.color;
            _challengeFlashImage.color = ScreenFlashRed;
            yield return new WaitForSeconds(ColorFlashTime);
            _challengeFlashImage.color = originalColor;
        }

        // Coroutine to make screen flash green
        private IEnumerator TurnScreenGreenAfterPress()
        {
            var originalColor = _challengeFlashImage.color;
            _challengeFlashImage.color = ScreenFlashGreen;
            yield return new WaitUntil(() =>
                ChallengeDuration() >= ChallengeStartDelayInSeconds + ChallengeTimeoutLimitInSeconds);
            _challengeFlashImage.color = originalColor;
        }
        
        private void TurnScreenGreenAfterWin()
        {
            _challengeFlashImage.color = ScreenFlashGreen;
        }
        
        private void TurnScreenRedAfterLose()
        {
            _challengeFlashImage.color = ScreenFlashRed;
        }

        // Create random sugar value as a server rpc
        [Rpc(SendTo.Server)]
        private void CreateRandomSugarServerRpc(RpcParams rpcParams = default)
        {
            _randomTimeSugar = Random.Range(-RandomTimeSugarWindow, RandomTimeSugarWindow);
        }
    }
}