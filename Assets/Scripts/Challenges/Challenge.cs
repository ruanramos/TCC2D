using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static Challenges.Questions.QuestionManager;
using static GameConstants;
using Random = UnityEngine.Random;

namespace Challenges
{
    public class Challenge : NetworkBehaviour, IEquatable<Challenge>
    {
        /*
         * Challenge lifetime is as follows:
         * 1. Challenge has a delay time before it starts
         * 2. Challenge has a rand() time sugar value added to it's delay time
         * 3. Challenge starts and players have time limit to finish it
         * 4. Challenge shows who won and how long it took for each player to finish it
         */

        public NetworkVariable<ulong> Client1Id { get; } = new();
        public NetworkVariable<ulong> Client2Id { get; } = new();

        public NetworkVariable<FixedString32Bytes> Client1Name { get; } = new();
        public NetworkVariable<FixedString32Bytes> Client2Name { get; } = new();

        public NetworkVariable<ChallengeType> Type { get; set; } = new();


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
        public GameObject pressCounter;
        private TextMeshProUGUI _pressCounterText;
        private GameObject _challengeInnerCanvasHeader;
        private TextMeshProUGUI _challengeInnerCanvasHeaderText;

        private double _challengeStartTime;

        private Image _challengeFlashImage;

        private double _randomTimeSugar;
        private double _timeUntilWinner;
        private double _timeUntilStart;

        private int _pressesCounter;

        private NetworkVariable<FixedString128Bytes> Query { get; set; } = new();
        private NetworkList<FixedString128Bytes> Answer { get; set; } = new();
        private GameObject _answerInput;
        private TMP_InputField _answerInputText;


        private void Awake()
        {
            GetReferences();

            _challengeOuterCanvas.SetActive(false);
            _challengeInnerCanvas.SetActive(false);
            _challengeTimeout.SetActive(false);
            _challengeInfo.SetActive(false);
        }

        private void GetReferences()
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
            _pressCounterText = pressCounter.GetComponent<TextMeshProUGUI>();
            _challengeInnerCanvasHeader = _challengeInnerCanvas.transform.GetChild(0).gameObject;
            _challengeInnerCanvasHeaderText = _challengeInnerCanvasHeader.GetComponent<TextMeshProUGUI>();
            _answerInput = _challengeInnerCanvas.transform.GetChild(3).gameObject;
            _answerInputText = _answerInput.GetComponent<TMP_InputField>();
        }

        private void Start()
        {
            _challengeStartTime = NetworkManager.Singleton.ServerTime.Time;
            _challengeHeader.text = Client1Id.Value == NetworkManager.LocalClient.ClientId
                ? $"{Client1Name.Value} X {Client2Name.Value}"
                : $"{Client2Name.Value} X {Client1Name.Value}";

            if (LocalClientInChallenge())
            {
                _challengeOuterCanvas.SetActive(true);
                _challengeInnerCanvas.SetActive(true);
                _challengeInfo.SetActive(true);
            }

            _timeUntilWinner = ChallengeTimeoutLimitInSeconds + ChallengeStartDelayInSeconds + _randomTimeSugar;
            _timeUntilStart = ChallengeStartDelayInSeconds + _randomTimeSugar;

            if (IsButtonPressChallenge())
            {
                _pressCounterText.text = $"Remaining Attempts: {MaxPressesAllowed}";
            }

            _answerInput.SetActive(false);
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
            if (IsButtonPressChallenge())
            {
                _challengeInnerCanvasHeaderText.text = InnerCanvasTitleKeyboardPressChallenge;
                if (Input.GetKeyDown(KeyCode.Space) &&
                    LocalClientInChallenge() &&
                    _pressesCounter < MaxPressesAllowed)
                {
                    UpdatePressesCounterClientRpc(
                        RpcTarget.Group(new[] { NetworkManager.Singleton.LocalClientId }, RpcTargetUse.Temp));

                    if (ChallengeDuration() > _timeUntilStart &&
                        ChallengeDuration() < _timeUntilWinner &&
                        IsInChallengeTime())
                    {
                        // Send player positive feedback
                        if (!_clientFinishTimestamps.ContainsKey(NetworkManager.Singleton.LocalClientId))
                        {
                            StartCoroutine(TurnScreenGreenAfterPress());
                            _pressCounterText.enabled = false;
                        }

                        SendKeyboardTimestampToServerServerRpc(NetworkManager.Singleton.ServerTime.Time,
                            Input.inputString);
                    }

                    if (ChallengeDuration() < ChallengeStartDelayInSeconds)
                    {
                        // Send player negative feedback
                        StartCoroutine(FlashScreenRed());
                        // Focus on the input field again
                        _answerInputText.Select();
                        _answerInputText.ActivateInputField();
                    }
                }
            }

            if (IsQuestionChallenge())
            {
                _challengeInnerCanvasHeaderText.text = InnerCanvasTitleQuestionChallenge;
                if (LocalClientInChallenge())
                {
                    if (ChallengeDuration() >= ChallengeStartDelayInSeconds &&
                        ChallengeDuration() < _timeUntilWinner &&
                        IsInChallengeTime() &&
                        _challengeInnerCanvasHeaderText.text.Equals(InnerCanvasTitleQuestionChallenge))
                    {
                        _challengeInnerCanvasHeaderText.text = Query.Value.ToString();
                    }

                    // Send the answer to clients with a client rpc
                    if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
                    {
                        if (ChallengeDuration() > _timeUntilStart &&
                            ChallengeDuration() < _timeUntilWinner &&
                            IsInChallengeTime())
                        {
                            // Check if the answer is correct
                            var playerAnswer = _answerInputText.text;
                            var correctAnswer = Answer.Contains(playerAnswer.ToLower());
                            if (correctAnswer)
                            {
                                TurnScreenGreen();
                                SendKeyboardTimestampToServerServerRpc(NetworkManager.Singleton.ServerTime.Time,
                                    Input.inputString);
                                _answerInputText.enabled = false;
                            }
                            else
                            {
                                StartCoroutine(FlashScreenRed());
                                _answerInputText.Select();
                                _answerInputText.ActivateInputField();
                            }
                        }
                    }
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
                if (IsQuestionChallenge())
                {
                    _answerInput.SetActive(true);
                    _answerInputText.Select();
                    _answerInputText.ActivateInputField();
                }
            }

            if (!IsInChallengeTime() && !IsInDelayTime() &&
                (_startIndicatorImage.enabled || _challengeTimeout.activeSelf))
            {
                _startIndicatorImage.enabled = false;
                _answerInput.SetActive(false);
                _challengeTimeout.SetActive(false);
            }

            // Check if client running this is involved in challenge
            if ((Client1Id.Value == 0 && Client2Id.Value == 0) || !LocalClientInChallenge() ||
                _challengeHeader.text.Equals($"{Client1Name.Value} X {Client2Name.Value}") ||
                _challengeHeader.text.Equals($"{Client2Name.Value} X {Client1Name.Value}")) return;

            // If client is involved in challenge, show canvas
            _challengeOuterCanvas.SetActive(true);
            _challengeInnerCanvas.SetActive(true);
            _challengeInfo.SetActive(true);

            // Make local client name appear first in challenge header
            _challengeHeader.text = Client1Id.Value == NetworkManager.LocalClient.ClientId
                ? $"{Client1Name.Value} X {Client2Name.Value}"
                : $"{Client2Name.Value} X {Client1Name.Value}";
        }

        private bool IsButtonPressChallenge()
        {
            return Type.Value == ChallengeType.KeyboardButtonPress;
        }

        private bool IsQuestionChallenge()
        {
            return Type.Value == ChallengeType.QuestionChallenge;
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
            if (IsQuestionChallenge())
            {
                _answerInput.SetActive(true);

                if (Query.Value.IsEmpty && Answer.Count == 0 && IsServer)
                {
                    CreateRandomQuestionServerRpc();
                }
            }

            CreateRandomSugarServerRpc();
        }

        public ulong DecideWinner()
        {
            switch (_clientFinishTimestamps.Count)
            {
                // Check if any player completed the challenge
                case 0:
                    print(
                        $"<color=#FF00AA>Challenge between {Client1Name.Value} and {Client2Name.Value} had no winner</color>");
                    return 0;

                // Check if both players completed the challenge
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
                $"<color=#FF00AA>Winner of challenge between {Client1Name.Value} and {Client2Name.Value} is {winner}</color>");
            return winner;
        }

        public bool Equals(Challenge other)
        {
            return (Client1Id.Value == other.Client1Id.Value && Client2Id.Value == other.Client2Id.Value ||
                    Client1Id.Value == other.Client2Id.Value && Client2Id.Value == other.Client1Id.Value);
        }

        public override string ToString()
        {
            var result = $"{Client1Name.Value} x {Client2Name.Value}: ";
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
                return;
            }

            print(
                $"Received {key} press from client {clientId}\n" +
                $"timestamp: {time}  --- Server time: {NetworkManager.Singleton.ServerTime.Time}");

            // Store timestamp of key press on server if it's not already stored
            _clientFinishTimestamps.TryAdd(clientId, time);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void UpdatePressesCounterClientRpc(RpcParams rpcParams = default)
        {
            _pressesCounter++;
            var pressesText = MaxPressesAllowed - _pressesCounter > 0
                ? $"{MaxPressesAllowed - _pressesCounter}"
                : "No more. Waiting other player...";
            _pressCounterText.text = $"Remaining Attempts: {pressesText}";
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
                TurnScreenGreen();
            }
            else
            {
                TurnScreenRed();
            }

            _pressCounterText.enabled = false;

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
            var win = winnerId == Client1Id.Value ? Client1Name.Value : Client2Name.Value;

            _challengeInfoText.text = winnerId == 0
                ? "No winner"
                : $"{Client1Name.Value}: {client1ReactionTimeText} \n" +
                  $"{Client2Name.Value}: {client2ReactionTimeText} \n\n" +
                  $"{win} wins the challenge!";

            if (!IsQuestionChallenge()) return;

            var acceptedAnswers = new StringBuilder();
            foreach (var answer in Answer)
            {
                acceptedAnswers.Append(answer).Append(", ");
            }

            // Remove last comma and space
            acceptedAnswers.Remove(acceptedAnswers.Length - 2, 2);
            _challengeInfoText.text += $"\n\nAccepted answers: {acceptedAnswers}";
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

        private void TurnScreenGreen()
        {
            _challengeFlashImage.color = ScreenFlashGreen;
        }

        private void TurnScreenRed()
        {
            _challengeFlashImage.color = ScreenFlashRed;
        }

        // Create random sugar value as a server rpc
        [Rpc(SendTo.Server)]
        private void CreateRandomSugarServerRpc(RpcParams rpcParams = default)
        {
            _randomTimeSugar = Random.Range(-RandomTimeSugarWindow, RandomTimeSugarWindow);
        }

        // Create random question as a server rpc
        [Rpc(SendTo.Server)]
        private void CreateRandomQuestionServerRpc(RpcParams rpcParams = default)
        {
            var question = GetRandomQuestion();
            Query.Value = question.query;
            print(Query.Value);
            foreach (var answer in question.answers)
            {
                Answer.Add(answer);
            }
        }
    }
}