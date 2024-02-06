using System.Collections;
using DefaultNamespace;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameConstants;

public class PlayerNetwork : NetworkBehaviour
{
    private NetworkTransform _transform;
    private TextMeshPro _playerLabelText;
    private TextMeshProUGUI _scoreText;
    private SpriteRenderer _challengeImage;
    private float _speedMultiplier = 1;


    private NetworkVariable<int> _score = new();
    private NetworkVariable<bool> _isInChallenge = new();
    private NetworkVariable<ulong> _challengeOpponent = new();
    private NetworkVariable<int> _lives = new(StartingLives);
    private NetworkVariable<Color> _color = new();


    private void Awake()
    {
        _scoreText = GameObject.Find("ScoreUI").GetComponentInChildren<TextMeshProUGUI>();
        _challengeImage = Utilities.FindChildGameObjectByName(transform, "Swords").GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        var player = gameObject;

        // Renaming gameobject for clarity
        player.name = $"Player {OwnerClientId}";

        _score.OnValueChanged += TreatScoreChanged;
        _isInChallenge.OnValueChanged += TreatInChallengeChanged;
        _challengeOpponent.OnValueChanged += TreatOpponentChanged;
        _lives.OnValueChanged += TreatLivesChanged;
        _color.OnValueChanged += TreatColorChanged;

        _playerLabelText = player.GetComponentInChildren<TextMeshPro>();
        UpdatePlayerLabel();
        _playerLabelText.color = Color.green;

        switch (IsOwner)
        {
            case false:
                _playerLabelText.color = Color.red;
                break;
            case true:
                print($"Applying starting score of 0 on owner (ownerclientid) {OwnerClientId}");
                _scoreText.text = "Score: 0";
                SetUpColorServerRpc();
                break;
        }

        gameObject.GetComponentInChildren<SpriteRenderer>().color = _color.Value;
    }

    [ServerRpc]
    private void SetUpColorServerRpc(ServerRpcParams serverRpcParams = default)
    {
        if (!IsServer) return;
        _color.Value = Random.ColorHSV();
        gameObject.GetComponentInChildren<SpriteRenderer>().color = _color.Value;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        _score.OnValueChanged -= TreatScoreChanged;
        _isInChallenge.OnValueChanged -= TreatInChallengeChanged;
        _challengeOpponent.OnValueChanged -= TreatOpponentChanged;
        _lives.OnValueChanged -= TreatLivesChanged;
        _color.OnValueChanged -= TreatColorChanged;
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
        //_challengeImage.enabled = _isInChallenge.Value;

        if (!IsOwner) return;

        var movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (movement.magnitude > 0 && !_isInChallenge.Value)
        {
            SendClientInputServerRpc(movement);
        }

        if (Input.anyKeyDown)
        {
            SendKeyboardTimestampToServerServerRpc(NetworkManager.Singleton.NetworkTimeSystem.ServerTime,
                Input.inputString);
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // This will run only on server side
        switch (other.gameObject.tag)
        {
            case "Collectible" when !IsServer:
                return;
            case "Collectible":
                _score.Value += CollectibleValue;
                break;
            case "Player":
                var opponentId = other.gameObject.GetComponent<NetworkBehaviour>().OwnerClientId;
                print(
                    $"Collision between players {OwnerClientId} and " +
                    $"{opponentId} happened");
                _challengeOpponent.Value = opponentId;
                print(
                    $"Changed {OwnerClientId} challenge opponent value to {_challengeOpponent.Value} at time {NetworkManager.Singleton.ServerTime.Time}");
                _isInChallenge.Value = true;

                //StartCoroutine(SimulateChallenge(gameObject, other.gameObject));
                break;
        }
    }

    private void TreatColorChanged(Color previousColor, Color currentColor)
    {
        if (!IsServer)
        {
            gameObject.GetComponentInChildren<SpriteRenderer>().color = currentColor;
        }
    }

    private void TreatOpponentChanged(ulong previousOpponent, ulong currentOpponent)
    {
        if (IsServer)
        {
            print($"Player {OwnerClientId} is now challenging player {currentOpponent}");
        }

        if (!IsServer && IsOwner)
        {
            StartChallengeInClient();
        }
    }

    private void TreatScoreChanged(int previousScore, int currentScore)
    {
        print($"Player {OwnerClientId} had a score of {previousScore}" +
              $" and now has a score of {currentScore}");
        if (IsServer) return;
        if (!IsOwner) return;
        _scoreText.text = $"Score: {currentScore}";
    }

    private void TreatInChallengeChanged(bool wasInChallenge, bool isInChallenge)
    {
        if (!isInChallenge)
        {
            // Left a challenge, remove challenge frame
            if (!IsOwner) return;
            GameManager.DestroyChallengeOuterCanvas();
            GameManager.DestroyChallengeInnerCanvas(ChallengeType.KeyboardButtonPress);
            return;
        }

        // Entered a challenge
        if (IsOwner)
        {
            //StartChallengeInClient();
        }

        gameObject.GetComponent<CircleCollider2D>().enabled = false;
        StartCoroutine(MakePlayerTransparentWhileInChallenge());
    }

    private void StartChallengeInClient()
    {
        // Create challenge outer frame
        var challengeOuterCanvas = GameManager.InstantiateChallengeOuterCanvas();
        print($"Creating challenge outer frame at time {NetworkManager.Singleton.ServerTime.Time}");
        print(
            $"Challenge opponent is {_challengeOpponent.Value} at time {NetworkManager.Singleton.ServerTime.Time}");
        challengeOuterCanvas.GetComponentInChildren<TextMeshProUGUI>().text =
            $"Player {OwnerClientId} X Player {_challengeOpponent.Value}";
        // Create challenge inner frame
        print($"Creating challenge inner frame at time {NetworkManager.Singleton.ServerTime.Time}");
        GameManager.InstantiateChallengeInnerCanvas(ChallengeType.KeyboardButtonPress);
        // Run challenge
        StartCoroutine(KeyboardButtonPressChallenge(gameObject,
            GameObject.Find($"Player {_challengeOpponent.Value}")));
    }

    private IEnumerator MakePlayerTransparentWhileInChallenge()
    {
        var playerRenderer = GetComponentInChildren<SpriteRenderer>();
        var color = playerRenderer.color;
        playerRenderer.color = new Color(color.r, color.g, color.b, 0.4f);
        yield return new WaitWhile(() => _isInChallenge.Value);
        _speedMultiplier = 2;
        yield return new WaitForSeconds(PostChallengeInvincibilityTimeInSeconds);
        gameObject.GetComponent<CircleCollider2D>().enabled = true;
        playerRenderer.color = color;
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
            print(
                $"Player {OwnerClientId} lost a challenge and lost 1 life." +
                $" It had {previousLives} Now it has {currentLives}");
        }


        if (currentLives > previousLives)
        {
            // Won challenge
            print(
                $"Player {OwnerClientId} won a challenge and earned 1 life." +
                $" It had {previousLives} Now it has {currentLives}");
            if (IsServer)
            {
                _score.Value += ChallengeValue;
            }
        }

        UpdatePlayerLabel();
    }

    private IEnumerator ButtonPressChallenge(GameObject player1, GameObject player2)
    {
        var player1NetworkBehaviour = player1.GetComponent<NetworkBehaviour>();
        var player2NetworkBehaviour = player2.GetComponent<NetworkBehaviour>();
        var player1Network = player1.GetComponent<PlayerNetwork>();
        var player2Network = player2.GetComponent<PlayerNetwork>();
        var player1Id = player1NetworkBehaviour.OwnerClientId;
        var player2Id = player2NetworkBehaviour.OwnerClientId;

        if (player1Network._isInChallenge.Value || player2Network._isInChallenge.Value)
        {
            print(
                $"Tried to start button press challenge between players {player1Id}" +
                $" and {player2Id}, but one of them is already in a challenge");
            yield break;
        }

        player1Network._isInChallenge.Value = true;
        player2Network._isInChallenge.Value = true;

        print($"Starting button press challenge between players {player1Id} and {player2Id}");

        Instantiate(Resources.Load<GameObject>($"Prefabs/{ChallengeType.ButtonPress}Canvas"));

        //yield return new WaitUntil(() => );
    }

    private IEnumerator KeyboardButtonPressChallenge(GameObject player1, GameObject player2)
    {
        var player1NetworkBehaviour = player1.GetComponent<NetworkBehaviour>();
        var player2NetworkBehaviour = player2.GetComponent<NetworkBehaviour>();
        var player1Network = player1.GetComponent<PlayerNetwork>();
        var player2Network = player2.GetComponent<PlayerNetwork>();
        var player1Id = player1NetworkBehaviour.OwnerClientId;
        var player2Id = player2NetworkBehaviour.OwnerClientId;

        if (player1Network._isInChallenge.Value || player2Network._isInChallenge.Value)
        {
            print(
                $"Tried to start keyboard button press challenge between players {player1Id}" +
                $" and {player2Id}, but one of them is already in a challenge");
            yield break;
        }

        print($"Starting keyboard button press challenge between players {player1Id} and {player2Id}");

        if (IsOwner)
        {
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        }

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
            $"Finishing keyboard button press challenge between players {player1Id}" +
            $" and {player2Id}. Player {winnerId} wins");
        print($"Removing player {loserId} life");
        loser.gameObject.GetComponent<PlayerNetwork>()._lives.Value -= 1;
        if (winner.gameObject.GetComponent<PlayerNetwork>()._lives.Value < MaxLives)
        {
            print($"Incrementing player {winnerId} life");
            winner.gameObject.GetComponent<PlayerNetwork>()._lives.Value += 1;
            yield break;
        }

        print($"Could not increment player {winnerId} lives because it is already at max lives");
    }

    private IEnumerator SimulateChallenge(GameObject player1, GameObject player2)
    {
        var player1NetworkBehaviour = player1.GetComponent<NetworkBehaviour>();
        var player2NetworkBehaviour = player2.GetComponent<NetworkBehaviour>();
        var player1Network = player1.GetComponent<PlayerNetwork>();
        var player2Network = player2.GetComponent<PlayerNetwork>();
        var player1Id = player1NetworkBehaviour.OwnerClientId;
        var player2Id = player2NetworkBehaviour.OwnerClientId;

        if (player1Network._isInChallenge.Value || player2Network._isInChallenge.Value)
        {
            print(
                $"Tried to start challenge simulation between players {player1Id}" +
                $" and {player2Id}, but one of them is already in a challenge");
            yield break;
        }

        print(
            $"Starting challenge simulation coroutine between players {player1Id}" +
            $" and {player2Id}");
        player1Network._isInChallenge.Value = true;
        player2Network._isInChallenge.Value = true;

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
        print($"Removing player {loserId} life");
        loser.gameObject.GetComponent<PlayerNetwork>()._lives.Value -= 1;
        if (winner.gameObject.GetComponent<PlayerNetwork>()._lives.Value < MaxLives)
        {
            print($"Incrementing player {winnerId} life");
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
    public void SendKeyboardTimestampToServerServerRpc(double time, string key)
    {
        print(
            $"Received {key} press from client {OwnerClientId}\n" +
            $"timestamp: {time}  --- Server time: {NetworkManager.Singleton.NetworkTimeSystem.ServerTime}");
    }
}