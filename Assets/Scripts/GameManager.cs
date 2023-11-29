using Unity.Netcode;
using UnityEngine;
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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //NetworkManager.Singleton.OnClientConnectedCallback += CreateClientData;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        //NetworkManager.Singleton.OnClientConnectedCallback -= CreateClientData;
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
        Physics.IgnoreLayerCollision((int)Player, (int)InChallengePlayer);
        print($"Removed collisions from layers {Player} and {InChallengePlayer}");
    }

    public static void ApplyClientColors()
    {
        foreach (var connectedPlayer in ConnectedPlayers)
        {
            Debug.Log($"Applying color {connectedPlayer.ClientColor} to player {connectedPlayer.ClientId}");
            foreach (var playerNetwork in FindObjectsOfType<PlayerNetwork>())
            {
                if (playerNetwork.OwnerClientId != connectedPlayer.ClientId) continue;
                playerNetwork.GetComponentInChildren<SpriteRenderer>().color = connectedPlayer.ClientColor;
                break;
            }
        }
    }
}