using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameConstants;
using Random = UnityEngine.Random;

public class GameManager : NetworkBehaviour
{
    public static NetworkList<PlayerData> ConnectedPlayers;
    private GameObject _mainCamera;
    private Camera _mainCameraCamera;
    private GameObject _devInfo;
    private GameObject _scoreList;
    private TextMeshProUGUI _devInfoText;
    private static TextMeshProUGUI _scoreListText;

    private GameObject _connectedPlayersText;

    private static ToggleGroup _challengeTypeToggleGroup;
    private static ChallengeType _currentChallengeType;

    private float fps;
    float updateTimer = 0.25f;

    private void Awake()
    {
        ConnectedPlayers = new NetworkList<PlayerData>();
        _devInfo = GameObject.Find("DevInfo");
        _scoreList = GameObject.Find("ScoreList");
        _devInfoText = _devInfo.GetComponent<TextMeshProUGUI>();
        _scoreListText = _scoreList.GetComponent<TextMeshProUGUI>();
        _challengeTypeToggleGroup = GameObject.Find("ChallengeModesToggleGroup").GetComponent<ToggleGroup>();

        if (Camera.main != null) _mainCamera = Camera.main.gameObject;
        _mainCameraCamera = _mainCamera.GetComponent<Camera>();

        _connectedPlayersText = GameObject.Find("ConnectedPlayersText");
        _connectedPlayersText.SetActive(false);
    }

    private void Update()
    {
        UpdateFPSDisplay();
        if (IsServer)
        {
            _mainCamera.transform.position +=
                new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0) *
                (Time.deltaTime * BaseObserverCameraMovespeed);

            _mainCameraCamera.orthographicSize -= Input.GetAxis("Mouse ScrollWheel") * 10;
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            _devInfo.SetActive(!_devInfo.activeSelf);
        }

        if (_devInfo.activeSelf && (IsServer || IsClient))
        {
            _devInfoText.text = $"Fps: {Mathf.Round(fps)}\n";
        }
    }

    private void Start()
    {
        _devInfo.SetActive(false);
        NetworkManager.NetworkConfig.ConnectionApproval = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApprovalCallback;
    }

    public static void Disconnect()
    {
        if (!NetworkManager.Singleton.IsClient) return;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        NetworkManager.Singleton.Shutdown();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _connectedPlayersText.SetActive(true);
        }

        NetworkManager.Singleton.OnConnectionEvent += (_, type) => { UpdateConnectedPlayersText(type.EventType); };
        _currentChallengeType = _challengeTypeToggleGroup.GetFirstActiveToggle().name switch
        {
            "RandomToggle" => ChallengeType.Random,
            "QuestionToggle" => ChallengeType.QuestionChallenge,
            "KeyboardPressToggle" => ChallengeType.KeyboardButtonPress,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static Dictionary<FixedString32Bytes, int> GetScores()
    {
        var scores = FindObjectsOfType<PlayerNetwork>()
            .ToDictionary(playerNetwork => playerNetwork.GetPlayerName(), playerNetwork => playerNetwork.GetScore());

        // Order the scores by value by using a IOrderedEnumerable
        var orderedScores = scores.OrderByDescending(pair => pair.Value)
            .Take(MaximumPlayersToDisplayScore)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        return orderedScores;
    }

    private static string ConstructHighscoreListString()
    {
        var scores = GetScores();
        var highscoreListString = $"Top {MaximumPlayersToDisplayScore}\n";
        var myEnumerator = scores.GetEnumerator();
        var count = 0;


        while (myEnumerator.MoveNext() && count < MaximumPlayersToDisplayScore)
        {
            // Get player name from player network
            highscoreListString += $"{myEnumerator.Current.Key} - {myEnumerator.Current.Value}\n";
            count++;
        }

        return highscoreListString;
    }

    public static void UpdateHighscoreList()
    {
        var highscoreListString = ConstructHighscoreListString();
        _scoreListText.text = highscoreListString;
    }

    private void UpdateConnectedPlayersText(ConnectionEvent type)
    {
        if (!IsServer) return;
        if (!_connectedPlayersText.activeSelf)
        {
            _connectedPlayersText.SetActive(true);
        }

        _connectedPlayersText.GetComponent<TextMeshProUGUI>().text = type switch
        {
            ConnectionEvent.ClientConnected =>
                $"Connected players: {NetworkManager.Singleton.ConnectedClientsList.Count()}\n",
            ConnectionEvent.ClientDisconnected =>
                $"Connected players: {NetworkManager.Singleton.ConnectedClientsList.Count() - 1}\n",
            _ => _connectedPlayersText.GetComponent<TextMeshProUGUI>().text
        };
    }

    private static void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Position = GetPlayerSpawnPosition();
    }

    private static Vector3 GetPlayerSpawnPosition()
    {
        return new Vector3(Random.Range(MinHorizontal, MaxHorizontal), Random.Range(MinVertical, MaxVertical), 0);
    }

    public static ChallengeType GetCurrentChallengeType()
    {
        return _currentChallengeType;
    }

    public static void DestroyToggleGroup()
    {
        Destroy(_challengeTypeToggleGroup.gameObject);
    }

    private void UpdateFPSDisplay()
    {
        updateTimer -= Time.deltaTime;
        if (!(updateTimer <= 0f)) return;
        fps = 1f / Time.unscaledDeltaTime;
        updateTimer = 0.25f;
    }
}