using System.Collections;
using Unity.Netcode;
using UnityEngine;
using static GameConstants;
using Random = UnityEngine.Random;

public class PlayerVisuals : NetworkBehaviour
{
    private NetworkVariable<Color> _color = new();
    private SpriteRenderer _spriteRenderer;
    private PlayerNetwork _playerNetwork;

    private void Awake()
    {
        _spriteRenderer = gameObject.GetComponentInChildren<SpriteRenderer>();
        _playerNetwork = gameObject.GetComponent<PlayerNetwork>();
    }

    private void TreatColorChanged(Color previousColor, Color currentColor)
    {
        if (!IsServer)
        {
            _spriteRenderer.color = currentColor;
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
        var color = _spriteRenderer.color;
        _spriteRenderer.color = new Color(color.r, color.g, color.b, PlayerAlphaWhileInChallenge);
        yield return new WaitWhile(() => _playerNetwork.GetIsInChallenge());
        yield return new WaitForSeconds(PostChallengeInvincibilityTimeInSeconds);
        _spriteRenderer.color = color;
    }

    [ServerRpc]
    private void SetUpColorServerRpc(ServerRpcParams serverRpcParams = default)
    {
        if (!IsServer) return;
        _color.Value = Random.ColorHSV();
        _spriteRenderer.color = _color.Value;
        print($"Applying color {_color.Value} at {NetworkManager.ServerTime.Time}");
    }
}