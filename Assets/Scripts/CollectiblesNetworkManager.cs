using Unity.Netcode;
using UnityEngine;
using static GameConstants;

public class CollectiblesNetworkManager : NetworkBehaviour
{
    [SerializeField] private GameObject collectiblePrefab;

    public static CollectiblesNetworkManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        InitialCollectibleSpawn();
    }

    private void InitialCollectibleSpawn()
    {
        for (var i = 0; i < NumberOfCollectibles; i++)
        {
            var collectibleTransform = Instantiate(collectiblePrefab,
                new Vector3(
                    Random.Range(MinHorizontal, MaxHorizontal),
                    Random.Range(MinVertical, MaxVertical),
                    0),
                collectiblePrefab.transform.rotation);
            collectibleTransform.GetComponent<NetworkObject>().Spawn(true);
        }
    }
}