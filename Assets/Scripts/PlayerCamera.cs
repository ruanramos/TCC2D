using Unity.Netcode;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        if (Camera.main == null) return;
        var mainCamera = Camera.main.gameObject;
        mainCamera.transform.parent = transform;
    }
}