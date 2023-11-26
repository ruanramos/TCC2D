using Unity.Netcode;
using UnityEngine;
using static GameConstants;

public class GameManager : NetworkBehaviour
{
    public static NetworkList<PlayerData> ConnectedPlayers;
    private bool _appliedColors;
    private GameObject _mainCamera;

    private void Awake()
    {
        ConnectedPlayers = new NetworkList<PlayerData>();
        _appliedColors = false;

        if (Camera.main != null) _mainCamera = Camera.main.gameObject;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        _appliedColors = false;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        var playerRandomColor = Random.ColorHSV();
        Debug.LogWarning($"Server assigned new player {clientId} color {playerRandomColor}");
        var newPlayerData = new PlayerData(clientId, playerRandomColor);
        ConnectedPlayers.Add(newPlayerData);
        print("Added new player to connected players list");
        _appliedColors = false;
    }

    private void Update()
    {
        if (IsServer)
        {
            _mainCamera.transform.position +=
                new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0) *
                (Time.deltaTime * BaseObserverCameraMovespeed);
        }

        if (_appliedColors) return;
        ApplyClientColors();
        _appliedColors = true;
    }

    private void Start()
    {
        print($"Will remove collisions from layers {PlayerLayerNumber} and {InChallengePlayerLayerNumber}");
        Physics.IgnoreLayerCollision(PlayerLayerNumber, InChallengePlayerLayerNumber);
        print($"Removed collisions from layers {PlayerLayerNumber} and {InChallengePlayerLayerNumber}");
        print(
            $"IS COLLISION SUPPOSED TO HAPPEN? {!Physics.GetIgnoreLayerCollision(InChallengePlayerLayerNumber, PlayerLayerNumber)}");
    }


    private static void ApplyClientColors()
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