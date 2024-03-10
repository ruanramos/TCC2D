using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameConstants;

public class GameManager : NetworkBehaviour
{
    public static NetworkList<PlayerData> ConnectedPlayers;
    private GameObject _mainCamera;
    private GameObject _devInfo;
    private GameObject _scoreList;
    private TextMeshProUGUI _devInfoText;
    private static TextMeshProUGUI _scoreListText;


    private void Awake()
    {
        ConnectedPlayers = new NetworkList<PlayerData>();
        _devInfo = GameObject.Find("DevInfo");
        _scoreList = GameObject.Find("ScoreList");
        _devInfoText = _devInfo.GetComponent<TextMeshProUGUI>();
        _scoreListText = _scoreList.GetComponent<TextMeshProUGUI>();

        if (Camera.main != null) _mainCamera = Camera.main.gameObject;
    }

    private void Update()
    {
        if (IsServer)
        {
            _mainCamera.transform.position +=
                new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0) *
                (Time.deltaTime * BaseObserverCameraMovespeed);

            _mainCamera.GetComponent<Camera>().orthographicSize -= Input.GetAxis("Mouse ScrollWheel") * 10;
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            _devInfo.SetActive(!_devInfo.activeSelf);
        }

        if (_devInfo.activeSelf && (IsServer || IsClient))
        {
            _devInfoText.text = $"Local time: {NetworkManager.Singleton.NetworkTimeSystem.LocalTime}\n" +
                                $"Server time: {NetworkManager.Singleton.NetworkTimeSystem.ServerTime}\n";
        }
    }

    private void Start()
    {
        _devInfo.SetActive(false);
    }

    public static void Disconnect()
    {
        if (!NetworkManager.Singleton.IsClient) return;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        NetworkManager.Singleton.Shutdown();
    }

    private static Dictionary<ulong, int> GetScores()
    {
        var scores = FindObjectsOfType<PlayerNetwork>()
            .ToDictionary(playerNetwork => playerNetwork.OwnerClientId, playerNetwork => playerNetwork.GetScore());

        // Order the scores by value by using a IOrderedEnumerable
        var orderedScores = scores.OrderByDescending(pair => pair.Value)
            .Take(MaximumPlayersToDisplayScore)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        return orderedScores;
    }

    private static string ConstructHighscoreListString()
    {
        var scores = GetScores();
        var highscoreListString = "Best Players:\n";
        var myEnumerator = scores.GetEnumerator();
        var count = 0;


        while (myEnumerator.MoveNext() && count < MaximumPlayersToDisplayScore)
        {
            highscoreListString += $"Player {myEnumerator.Current.Key} - {myEnumerator.Current.Value}\n";
            count++;
        }

        return highscoreListString;
    }

    public static void UpdateHighscoreList()
    {
        var highscoreListString = ConstructHighscoreListString();
        print($"{highscoreListString}");
        _scoreListText.text = highscoreListString;
        print("AAAAAAAA");
    }
}