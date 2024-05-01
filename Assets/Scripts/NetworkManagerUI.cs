using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUi : MonoBehaviour
{
    [SerializeField] private Button serverButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button disconnectButton;

    private GameObject _playerNicknameObject;
    private TMP_InputField _playerNicknameInputField;

    private void Awake()
    {
        _playerNicknameObject = GameObject.Find("NicknameInput");
        _playerNicknameInputField = _playerNicknameObject.GetComponent<TMP_InputField>();
        _playerNicknameInputField.Select();
        _playerNicknameInputField.ActivateInputField();

        serverButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
            serverButton.image.color = Color.green;
            Destroy(_playerNicknameObject);
            GameManager.DestroyToggleGroup();
            ActivateDisconnectButton();
        });

        hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            hostButton.image.color = Color.green;
            Destroy(_playerNicknameObject);
            GameManager.DestroyToggleGroup();
            ActivateDisconnectButton();
        });

        clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.OnConnectionEvent += (_, data) =>
            {
                if (data.EventType != ConnectionEvent.ClientConnected)
                {
                    return;
                }

                var playerNickname = _playerNicknameObject.GetComponent<TMP_InputField>().text;
                NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetwork>()
                    .SetPlayerNameServerRpc(playerNickname);
                clientButton.image.color = Color.green;
                Destroy(_playerNicknameObject);
                GameManager.DestroyToggleGroup();
            };
            NetworkManager.Singleton.StartClient();
            ActivateDisconnectButton();
        });

        disconnectButton.onClick.AddListener(DisconnectUiBehavior);

        GameObject.Find("PlayerScore").GetComponent<TextMeshProUGUI>().text = "";
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Return)) return;
        if (!NetworkManager.Singleton.IsClient || !NetworkManager.Singleton.IsServer)
        {
            clientButton.onClick.Invoke();
        }
    }

    private void DisconnectUiBehavior()
    {
        GameManager.Disconnect();

        NetworkManager.Singleton.Shutdown();
        ButtonColoring();
        DeactivateButtons(disconnectButton);
        ActivateButtons(clientButton, hostButton, serverButton);
    }

    private void ButtonColoring()
    {
        hostButton.image.color = disconnectButton.image.color;
        clientButton.image.color = disconnectButton.image.color;
        serverButton.image.color = disconnectButton.image.color;
    }

    private void ActivateDisconnectButton()
    {
        DeactivateButtons(clientButton, hostButton, serverButton);
        ActivateButtons(disconnectButton);
    }

    private static void DeactivateButtons(params Button[] buttons)
    {
        foreach (var button in buttons)
        {
            button.interactable = false;
        }
    }

    private static void ActivateButtons(params Button[] buttons)
    {
        foreach (var button in buttons)
        {
            button.interactable = true;
        }
    }
}