using Unity.Netcode;
using UnityEngine;
using static GameConstants;

public class PlayerCamera : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        var mainCamera = GameObject.Find("MainCamera");
        mainCamera.transform.parent = transform;
        mainCamera.transform.localPosition = new Vector3(transform.position.x, transform.position.y, CameraHeight);
    }
}