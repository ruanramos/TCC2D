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
    private TextMeshProUGUI _devInfoText;


    private void Awake()
    {
        ConnectedPlayers = new NetworkList<PlayerData>();
        _devInfo = GameObject.Find("DevInfo");
        _devInfoText = _devInfo.GetComponent<TextMeshProUGUI>();

        if (Camera.main != null) _mainCamera = Camera.main.gameObject;
    }

    private void CreateClientData(ulong clientId)
    {
        if (!IsServer) return;
        var playerRandomColor = Random.ColorHSV();
        Debug.LogWarning($"Server assigned new player {clientId} color {playerRandomColor}");
        var newPlayerData = new PlayerData(clientId, playerRandomColor);
        ConnectedPlayers.Add(newPlayerData);
        print("Added new player to connected players list");
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
        //Physics.IgnoreLayerCollision((int)Player, (int)InChallengePlayer);
        //print($"Removed collisions from layers {Player} and {InChallengePlayer}");
    }

    public static Transform InstantiateChallengeOuterCanvas()
    {
        return Instantiate(Resources.Load<GameObject>("Prefabs/ChallengeOuterCanvas")).transform;
    }

    public static void DestroyChallengeOuterCanvas()
    {
        Destroy(GameObject.Find("ChallengeOuterCanvas(Clone)"));
    }

    public static Transform InstantiateChallengeInnerCanvas(ChallengeType challengeType)
    {
        return Instantiate(Resources.Load<GameObject>($"Prefabs/{(ChallengeType)((int)challengeType)}ChallengeCanvas")).transform;
        //return Instantiate(Resources.Load<GameObject>($"Prefabs/KeyboardButtonPressChallengeCanvas")).transform;
    }

    public static void DestroyChallengeInnerCanvas(ChallengeType challengeType)
    {
        Destroy(GameObject.Find($"{challengeType}Canvas(Clone)"));
    }

    public static void Disconnect()
    {
        if (!NetworkManager.Singleton.IsClient) return;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        NetworkManager.Singleton.Shutdown();
    }
}