using Unity.Netcode;
using UnityEngine;
using static GameConstants;

public class CollectibleBehaviour : NetworkBehaviour
{
    public void OnCollisionEnter2D(Collision2D other)
    {
        if (!other.gameObject.tag.Equals("Player")) return;
        MoveCollectible();
    }

    private void MoveCollectible()
    {
        gameObject.GetComponent<NetworkObject>().Despawn(destroy: false);
        transform.position = new Vector3(
            Random.Range(MinHorizontal, MaxHorizontal),
            Random.Range(MinVertical, MaxVertical),
            0);
        gameObject.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
    }
}