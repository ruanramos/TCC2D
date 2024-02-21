using System.Collections;
using Unity.Netcode;
using UnityEngine;
using static GameConstants;

public class PlayerVisuals : NetworkBehaviour
{
    private NetworkVariable<Color> _color = new();

    private void TreatColorChanged(Color previousColor, Color currentColor)
    {
        if (!IsServer)
        {
            gameObject.GetComponentInChildren<SpriteRenderer>().color = currentColor;
        }
    }

    public override void OnNetworkSpawn()
    {
        _color.OnValueChanged += TreatColorChanged;
        if (IsOwner) SetUpColorServerRpc();
        else gameObject.GetComponentInChildren<SpriteRenderer>().color = _color.Value;
    }

    public override void OnNetworkDespawn()
    {
        _color.OnValueChanged -= TreatColorChanged;
    }

    public IEnumerator MakePlayerTransparentWhileInChallenge()
    {
        var playerRenderer = GetComponentInChildren<SpriteRenderer>();
        var color = playerRenderer.color;
        playerRenderer.color = new Color(color.r, color.g, color.b, PlayerAlphaWhileInChallenge);
        yield return new WaitWhile(() => GetComponent<PlayerNetwork>().GetIsInChallenge());
        yield return new WaitForSeconds(PostChallengeInvincibilityTimeInSeconds);
        playerRenderer.color = color;
    }

    [ServerRpc]
    private void SetUpColorServerRpc(ServerRpcParams serverRpcParams = default)
    {
        print($"Entered set up color server rpc at {NetworkManager.ServerTime.Time}");
        if (!IsServer) return;
        _color.Value = Random.ColorHSV();
        gameObject.GetComponentInChildren<SpriteRenderer>().color = _color.Value;
        print($"Applying color {_color.Value} at {NetworkManager.ServerTime.Time}");
    }
}