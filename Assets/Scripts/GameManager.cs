using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameConstants;
using static GameConstants.Layers;

public class GameManager : NetworkBehaviour
{
    public static NetworkList<PlayerData> ConnectedPlayers;
    private GameObject _mainCamera;

    private void Awake()
    {
        ConnectedPlayers = new NetworkList<PlayerData>();

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
        }
    }

    private void Start()
    {
        //Physics.IgnoreLayerCollision((int)Player, (int)InChallengePlayer);
        //print($"Removed collisions from layers {Player} and {InChallengePlayer}");
    }

    public static Transform InstantiateChallengeCanvas()
    {
        return Instantiate(Resources.Load<GameObject>("Prefabs/ChallengeCanvas")).transform;
    }

    public static void DestroyChallengeCanvas()
    {
        Destroy(GameObject.Find("ChallengeCanvas"));
    }
    
    public static void Disconnect()
    {
        if (!NetworkManager.Singleton.IsClient) return;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        NetworkManager.Singleton.Shutdown();
    }
}