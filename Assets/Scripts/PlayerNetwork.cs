using System.Collections;
using Challenges;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Challenges.ChallengeNetwork;
using static GameConstants;

public class PlayerNetwork : NetworkBehaviour
{
    private NetworkTransform _transform;
    private TextMeshPro _playerLabelText;
    private TextMeshProUGUI _scoreText;
    private float _speedMultiplier = 1;
    private PlayerVisuals _visuals;


    private NetworkVariable<int> _score = new();
    private NetworkVariable<bool> _isInChallenge = new();
    private NetworkVariable<ulong> _challengeOpponent = new();
    private GameObject _currentChallenge;
    private NetworkVariable<int> _lives = new(StartingLives);


    private void Awake()
    {
        _scoreText = GameObject.Find("ScoreUI").GetComponentInChildren<TextMeshProUGUI>();
        _visuals = gameObject.AddComponent<PlayerVisuals>();
    }

    public override void OnNetworkSpawn()
    {
        var player = gameObject;

        // Renaming gameobject for clarity in hierarchy
        player.name = $"Player {OwnerClientId}";

        _score.OnValueChanged += TreatScoreChanged;
        _isInChallenge.OnValueChanged += TreatInChallengeChanged;
        _challengeOpponent.OnValueChanged += TreatOpponentChanged;
        _lives.OnValueChanged += TreatLivesChanged;

        _playerLabelText = player.GetComponentInChildren<TextMeshPro>();
        UpdatePlayerLabel();
        _playerLabelText.color = Color.blue;

        switch (IsOwner)
        {
            case false:
                _playerLabelText.color = Color.red;
                break;
            case true:
                print($"Applying starting score of 0 on owner (ownerclientid) {OwnerClientId}");
                _scoreText.text = "Score: 0";
                break;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _score.OnValueChanged -= TreatScoreChanged;
        _isInChallenge.OnValueChanged -= TreatInChallengeChanged;
        _challengeOpponent.OnValueChanged -= TreatOpponentChanged;
        _lives.OnValueChanged -= TreatLivesChanged;
    }

    [ServerRpc]
    private void SendClientInputServerRpc(Vector2 inputValue, ServerRpcParams serverRpcParams = default)
    {
        if (inputValue.magnitude > 1)
        {
            inputValue.Normalize();
        }

        transform.position += new Vector3(inputValue.x, inputValue.y) *
                              (Time.deltaTime * BaseMovespeed * _speedMultiplier);
    }

    private void Update()
    {
        if (!IsOwner) return;

        var movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (movement.magnitude > 0 && !_isInChallenge.Value)
        {
            SendClientInputServerRpc(movement);
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // This will run only on server side
        switch (other.gameObject.tag)
        {
            case CollectibleTag:
                _score.Value += CollectibleValue;
                break;
            case PlayerTag:
                var opponentId = other.gameObject.GetComponent<NetworkBehaviour>().OwnerClientId;
                print(
                    $"Collision between players {OwnerClientId} and " +
                    $"{opponentId} happened");
                _challengeOpponent.Value = opponentId;
                print(
                    $"Changed {OwnerClientId} challenge opponent value to {_challengeOpponent.Value} at time {NetworkManager.Singleton.ServerTime.Time}");
                _isInChallenge.Value = true;
                StartCoroutine(SimulateChallenge(gameObject, other.gameObject));
                break;
        }
    }

    private void TreatOpponentChanged(ulong previousOpponent, ulong currentOpponent)
    {
        if (IsServer)
        {
            print(
                $"Player {OwnerClientId} challenge opponent changed from {previousOpponent} to {currentOpponent} at time {NetworkManager.Singleton.ServerTime.Time}");
        }
    }

    private void TreatScoreChanged(int previousScore, int currentScore)
    {
        if (IsServer)
        {
            print($"Player {OwnerClientId} had a score of {previousScore}" +
                  $" and now has a score of {currentScore}");
            return;
        }

        if (!IsOwner) return;
        _scoreText.text = $"Score: {currentScore}";
    }

    private void TreatInChallengeChanged(bool wasInChallenge, bool isInChallenge)
    {
        if (IsServer)
        {
            print(
                $"Player {OwnerClientId} isInChallenge changed from {wasInChallenge} to {isInChallenge} at time {NetworkManager.Singleton.ServerTime.Time}");
        }

        if (!isInChallenge)
        {
            StartCoroutine(PlayerPostChallengeBehavior(PostChallengeSpeedMultiplier));
            if (IsServer)
            {
                // Server will despawn and destroy challenge object
                if (ChallengeExists(OwnerClientId, _challengeOpponent.Value))
                {
                    RemoveChallenge(_currentChallenge);
                    _currentChallenge.GetComponent<NetworkObject>().Despawn();
                    print(
                        $"Despawned challenge network object for players {OwnerClientId} and {_challengeOpponent.Value} at time {NetworkManager.Singleton.ServerTime.Time}");
                    _challengeOpponent.Value = 0;
                }
            }

            if (!IsOwner) return;
            return;
        }

        // Entered a challenge
        print($"Player {OwnerClientId} entered a challenge at time {NetworkManager.Singleton.ServerTime.Time}");
        gameObject.GetComponent<CircleCollider2D>().enabled = false;
        gameObject.GetComponent<NetworkRigidbody2D>().enabled = false;

        if (IsServer && !ChallengeExists(OwnerClientId, _challengeOpponent.Value) && _challengeOpponent.Value != 0)
        {
            // Server will create challenge object and spawn
            _currentChallenge = CreateAndSpawnChallenge(OwnerClientId, _challengeOpponent.Value);
        }

        StartCoroutine(_visuals.MakePlayerTransparentWhileInChallenge());
    }

    private IEnumerator PlayerPostChallengeBehavior(float multiplier)
    {
        _speedMultiplier = multiplier;
        yield return new WaitForSeconds(PostChallengeInvincibilityTimeInSeconds);
        gameObject.GetComponent<CircleCollider2D>().enabled = true;
        gameObject.GetComponent<NetworkRigidbody2D>().enabled = true;
        _speedMultiplier = 1;
    }

    private void TreatLivesChanged(int previousLives, int currentLives)
    {
        if (currentLives == 0)
        {
            // Player is dead after challenge
            if (IsServer)
            {
                print($"Player {OwnerClientId} died");
            }

            if (IsOwner)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                NetworkManager.Singleton.Shutdown();
            }
        }

        if (currentLives < previousLives)
        {
            // Lost challenge
            if (IsServer)
            {
                print(
                    $"Player {OwnerClientId} lost a challenge and lost 1 life." +
                    $" It had {previousLives} Now it has {currentLives}");
            }
        }

        if (currentLives > previousLives)
        {
            // Won challenge
            if (IsServer)
            {
                print(
                    $"Player {OwnerClientId} won a challenge and earned 1 life." +
                    $" It had {previousLives} Now it has {currentLives}");
                _score.Value += ChallengeValue;
            }
        }

        UpdatePlayerLabel();
    }

    private IEnumerator SimulateChallenge(GameObject player1, GameObject player2)
    {
        var player1NetworkBehaviour = player1.GetComponent<NetworkBehaviour>();
        var player2NetworkBehaviour = player2.GetComponent<NetworkBehaviour>();
        var player1Network = player1.GetComponent<PlayerNetwork>();
        var player2Network = player2.GetComponent<PlayerNetwork>();
        var player1Id = player1NetworkBehaviour.OwnerClientId;
        var player2Id = player2NetworkBehaviour.OwnerClientId;

        yield return new WaitForSeconds(ChallengeSimulationTimeInSeconds);

        var winner = Random.Range(0, 2) == 0
            ? player1
            : player2;
        var loser = winner == player2
            ? player1
            : player2;

        player1Network._isInChallenge.Value = false;
        player2Network._isInChallenge.Value = false;

        var loserId = loser.GetComponent<NetworkBehaviour>().OwnerClientId;
        var winnerId = winner.GetComponent<NetworkBehaviour>().OwnerClientId;
        print(
            $"Finishing challenge simulation between players {player1Id}" +
            $" and {player2Id}. Player {winnerId} wins");
        loser.gameObject.GetComponent<PlayerNetwork>()._lives.Value -= 1;
        if (winner.gameObject.GetComponent<PlayerNetwork>()._lives.Value < MaxLives)
        {
            winner.gameObject.GetComponent<PlayerNetwork>()._lives.Value += 1;
            yield break;
        }

        print($"Could not increment player {winnerId} lives because it is already at max lives");
    }

    private void UpdatePlayerLabel()
    {
        print($"Updating player {OwnerClientId} label");
        _playerLabelText.text = $"Player: {OwnerClientId} \nLives: {_lives.Value}";
    }

    [ServerRpc]
    public void AddLivesServerRpc(int n)
    {
        _lives.Value += n;
    }

    [ServerRpc]
    public void RemoveLivesServerRpc(int n)
    {
        _lives.Value -= n;
    }

    [ServerRpc]
    public void AddScoreServerRpc(int n)
    {
        _score.Value += n;
    }

    [ServerRpc]
    public void RemoveScoreServerRpc(int n)
    {
        _score.Value -= n;
    }

    public bool GetIsInChallenge()
    {
        return _isInChallenge.Value;
    }

    public bool SetIsInChallenge(bool value)
    {
        _isInChallenge.Value = value;
        return _isInChallenge.Value;
    }
}