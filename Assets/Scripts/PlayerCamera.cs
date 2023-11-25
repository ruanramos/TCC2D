using Unity.Netcode;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        var mainCamera = GameObject.Find("Main Camera");
        mainCamera.transform.parent = transform;
    }
}