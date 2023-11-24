using Unity.Netcode;
using UnityEngine;
using static GameConstants;

public class CollectibleBehaviour : NetworkBehaviour
{
    public void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.tag.Equals("Player")) return;
        MoveCollectible();
    }

    private void MoveCollectible()
    {
        gameObject.GetComponent<NetworkObject>().Despawn(destroy: false);
        transform.position = new Vector3(
            Random.Range(MinHorizontal, MaxHorizontal),
            0,
            Random.Range(MinVertical, MaxVertical));
        gameObject.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
    }
    
}