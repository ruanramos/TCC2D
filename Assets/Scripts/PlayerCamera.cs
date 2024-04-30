using Unity.Netcode;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    private void Start()
    {
        if (!IsLocalPlayer) return;
        Transform transform1;
        (transform1 = Camera.main!.transform).SetParent(transform);
        transform1.localPosition = new Vector3(0, 0, -10);
    }
}