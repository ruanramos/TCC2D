using System.Collections;
using DefaultNamespace;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using static GameConstants;
using static GameConstants.Layers;

public class PlayerNetwork : NetworkBehaviour
{
    private NetworkTransform _transform;
    private TextMeshPro _playerLabelText;
    private TextMeshProUGUI _scoreText;
    private SpriteRenderer _challengeImage;
    private float _speedMultiplier = 1;


    private NetworkVariable<int> _score = new();
    private NetworkVariable<bool> _isInChallenge = new();
    private NetworkVariable<int> _lives = new(StartingLives);


    private void Awake()
    {
        _scoreText = GameObject.Find("ScoreUI").GetComponentInChildren<TextMeshProUGUI>();
        _challengeImage = Utilities.FindChildGameObjectByName(transform, "Swords").GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        var player = gameObject;

        // Renaming gameobject for clarity
        player.name = $"Player {NetworkManager.LocalClientId}";


        _score.OnValueChanged += TreatCollectibleCollision;
        _isInChallenge.OnValueChanged += TreatPlayerCollision;
        _lives.OnValueChanged += TreatLivesChanged;


        _playerLabelText = player.GetComponentInChildren<TextMeshPro>();
        UpdatePlayerLabel(player);
        _playerLabelText.color = Color.green;

        if (!IsOwner)
        {
            _playerLabelText.color = Color.red;
        }

        if (IsServer) return;
        print($"Applying starting score of 0 on owner (ownerclientid) {OwnerClientId}");
        _scoreText.text = "Score: 0";
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        _score.OnValueChanged -= TreatCollectibleCollision;
        _isInChallenge.OnValueChanged -= TreatPlayerCollision;
        _lives.OnValueChanged -= TreatLivesChanged;
    }

    [ServerRpc]
    private void SendClientInputServerRpc(Vector2 inputValue, ServerRpcParams serverRpcParams = default)
    {
        if (inputValue.magnitude > 1)
        {
            inputValue.Normalize();
        }

        transform.position += new Vector3(inputValue.x, inputValue.y) * (Time.deltaTime * BaseMovespeed * _speedMultiplier);
    }

    private void Update()
    {
        //_challengeImage.enabled = _isInChallenge.Value;

        if (!IsOwner) return;

        var movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (movement.magnitude > 0 && !_isInChallenge.Value)
        {
            SendClientInputServerRpc(movement);
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        switch (other.gameObject.tag)
        {
            case "Collectible" when !IsServer:
                return;
            case "Collectible":
                _score.Value += CollectibleValue;
                break;
            case "Player":
                print(
                    $"Collision between players {NetworkObjectId} and " +
                    $"{other.gameObject.GetComponent<NetworkBehaviour>().NetworkObjectId} happened");

                StartCoroutine(SimulateChallenge(gameObject, other.gameObject));

                var playerGameObject = gameObject;

                playerGameObject.layer = (int)InChallengePlayer;

                break;
        }
    }

    private void TreatCollectibleCollision(int previousScore, int currentScore)
    {
        print($"Player {NetworkObjectId} had a score of {previousScore}" +
              $" and now has a score of {currentScore}");
        if (IsServer) return;
        if (!IsOwner) return;
        _scoreText.text = $"Score: {currentScore}";
    }

    private void TreatPlayerCollision(bool wasInChallenge, bool isInChallenge)
    {
        UpdatePlayerLabel(gameObject);
        if (!isInChallenge) return;
        // Entered a challenge, collider and color behavior
        gameObject.GetComponent<CircleCollider2D>().enabled = false;
        StartCoroutine(MakePlayerTransparentWhileInChallenge());
    }

    private IEnumerator MakePlayerTransparentWhileInChallenge()
    {
        var renderer = GetComponentInChildren<SpriteRenderer>();
        var color = renderer.color;
        renderer.color = new Color(color.r, color.g, color.b, 0.4f);
        yield return new WaitWhile(() => _isInChallenge.Value);
        _speedMultiplier = 2;
        yield return new WaitForSeconds(PostChallengeInvincibilityTimeInSeconds);
        gameObject.GetComponent<CircleCollider2D>().enabled = true;
        renderer.color = color;
        _speedMultiplier = 1;
    }

    private IEnumerator PostChallengeInvincibility()
    {
        yield return new WaitForSeconds(PostChallengeInvincibilityTimeInSeconds);
        gameObject.GetComponent<CircleCollider2D>().enabled = true;
    }

    private void TreatLivesChanged(int previousLives, int currentLives)
    {
        if (currentLives <= 0)
        {
            // Player is dead after challenge
            print($"Player {NetworkObjectId} is dead");
            return;
        }

        if (currentLives < previousLives)
        {
            // Lost challenge
            print(
                $"Player {NetworkObjectId} lost a challenge and lost 1 life." +
                $" It had {previousLives} Now it has {currentLives}");
        }


        if (currentLives > previousLives)
        {
            // Won challenge
            print(
                $"Player {NetworkObjectId} won a challenge and earned 1 life." +
                $" It had {previousLives} Now it has {currentLives}");
        }

        UpdatePlayerLabel(gameObject);
    }

    private IEnumerator SimulateChallenge(GameObject player1, GameObject player2)
    {
        var player1NetworkBehaviour = player1.GetComponent<NetworkBehaviour>();
        var player2NetworkBehaviour = player2.GetComponent<NetworkBehaviour>();
        var player1Network = player1.GetComponent<PlayerNetwork>();
        var player2Network = player2.GetComponent<PlayerNetwork>();
        var player1Id = player1NetworkBehaviour.NetworkObjectId;
        var player2Id = player2NetworkBehaviour.NetworkObjectId;

        if (player1Network._isInChallenge.Value || player2Network._isInChallenge.Value)
        {
            print(
                $"Tried to start challenge simulation coroutine between players {player1Id}" +
                $" and {player2Id}, but one of them is already in a challenge");
            yield break;
        }

        print(
            $"Starting challenge simulation coroutine between players {player1Id}" +
            $" and {player2Id}");
        player1Network._isInChallenge.Value = true;
        player2Network._isInChallenge.Value = true;

        print($"Player {player1Id} was in layer {player1.layer} before challenge started");
        print($"Player {player2Id} was in layer {player2.layer} before challenge started");
        player1.layer = (int)InChallengePlayer;
        player2.layer = (int)InChallengePlayer;
        print($"Player {player1Id} is now in layer {player1.layer} after challenge started");
        print($"Player {player2Id} is now in layer {player2.layer} after challenge started");
        UpdatePlayerLabel(player1);
        UpdatePlayerLabel(player2);
        yield return new WaitForSeconds(ChallengeSimulationTimeInSeconds);

        print($"Player {player1Id} was in layer {player1.layer} before challenge ends");
        print($"Player {player2Id} was in layer {player2.layer} before challenge ends");
        player1.layer = (int)Player;
        player2.layer = (int)Player;
        print($"Player {player1Id} is now in layer {player1.layer} after challenge ends");
        print($"Player {player2Id} is now in layer {player2.layer} after challenge ends");
        UpdatePlayerLabel(player1);
        UpdatePlayerLabel(player2);

        var winner = Random.Range(0, 2) == 0
            ? player1
            : player2;
        var loser = winner == player2
            ? player1
            : player2;

        player1Network._isInChallenge.Value = false;
        player2Network._isInChallenge.Value = false;

        print(
            $"Finishing challenge simulation coroutine between players {player1Id}" +
            $" and {player2Id}. Player {winner} wins");


        var loserId = loser.GetComponent<NetworkBehaviour>().NetworkObjectId;
        var winnerId = winner.GetComponent<NetworkBehaviour>().NetworkObjectId;
        print($"Removing player {loserId} life");
        loser.gameObject.GetComponent<PlayerNetwork>()._lives.Value -= 1;
        print($"Incrementing player {winnerId} life");
        winner.gameObject.GetComponent<PlayerNetwork>()._lives.Value += 1;
    }

    private void UpdatePlayerLabel(GameObject player)
    {
        print($"Updating player {NetworkObjectId} label");
        _playerLabelText.text =
            $"Player: {NetworkObjectId} \nLives: {_lives.Value} \nLayer: {player.layer}";
        _playerLabelText.text = $"Player: {NetworkObjectId} \nLives: {_lives.Value}";
    }
}