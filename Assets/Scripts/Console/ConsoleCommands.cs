using QFSW.QC;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Console
{
    public class ConsoleCommands
    {
        [Command("connectedPlayers", "Prints the list of connected players")]
        public static void ConnectedPlayers()
        {
            foreach (var player in GameManager.ConnectedPlayers)
            {
                Debug.Log($"Player {player.ClientId} with color {player.ClientColor}");
            }
        }

        [Command("applyColors"), CommandDescription("Applies colors to all players")]
        public static void ApplyColors()
        {
            foreach (var player in GameManager.ConnectedPlayers)
            {
                Debug.Log($"Applying color {player.ClientColor} to player {player.ClientId}");
                foreach (var playerNetwork in Object.FindObjectsOfType<PlayerNetwork>())
                {
                    if (playerNetwork.OwnerClientId != player.ClientId) continue;
                    playerNetwork.GetComponentInChildren<MeshRenderer>().material.color = player.ClientColor;
                    break;
                }
            }
        }

        [Command("score", "")]
        public static void TestCommand()
        {
            var scoreText = GameObject.Find("ScoreUI").GetComponentInChildren<TextMeshProUGUI>();
            Debug.Log($"{scoreText.text}");
            Debug.Log($"{scoreText.enabled}");
        }

        [Command("cc", "instantiate challenge canvas")]
        public static void InstantiateChallengeCanvas()
        {
            var challengeCanvas = GameManager.InstantiateChallengeOuterCanvas();
        }

        [Command("dcc", "destroy challenge canvas")]
        public static void DestroyChallengeCanvas()
        {
            GameManager.DestroyChallengeCanvas();
        }
        
        [Command("addLives", "Add life to player")]
        public static void AddLives(ulong clientId, int n)
        {
            foreach (var playerNetwork in Object.FindObjectsOfType<PlayerNetwork>())
            {
                if (playerNetwork.OwnerClientId != clientId) continue;
                playerNetwork.AddLivesServerRpc(n);
                break;
            }
        }
        
        [Command("removeLives", "Add life to player")]
        public static void RemoveLives(ulong clientId, int n)
        {
            foreach (var playerNetwork in Object.FindObjectsOfType<PlayerNetwork>())
            {
                if (playerNetwork.OwnerClientId != clientId) continue;
                playerNetwork.RemoveLivesServerRpc(n);
                break;
            }
        }
    }
}