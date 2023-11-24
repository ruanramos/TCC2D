using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkManagerUi : MonoBehaviour
{
    [SerializeField] private Button serverButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button disconnectButton;

    private void Awake()
    {
        serverButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
            serverButton.image.color = Color.green;
            ActivateDisconnectButton();
        });
        clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            clientButton.image.color = Color.green;
            ActivateDisconnectButton();
        });
        hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            hostButton.image.color = Color.green;
            ActivateDisconnectButton();
        });

        disconnectButton.onClick.AddListener(() =>
        {
            if (NetworkManager.Singleton.IsClient)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                NetworkManager.Singleton.Shutdown();
            }

            NetworkManager.Singleton.Shutdown();
            hostButton.image.color = disconnectButton.image.color;
            clientButton.image.color = disconnectButton.image.color;
            serverButton.image.color = disconnectButton.image.color;
            DeactivateButtons(disconnectButton);
            ActivateButtons(clientButton, hostButton, serverButton);
        });


        GameObject.Find("PlayerScore").GetComponent<TextMeshProUGUI>().text = "";
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