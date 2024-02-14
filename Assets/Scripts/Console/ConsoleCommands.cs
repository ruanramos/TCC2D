using Challenges;
using QFSW.QC;
using TMPro;
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
            GameManager.DestroyChallengeOuterCanvas();
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

        [Command("challenges", "Prints the challenges happening")]
        public static void ChallengesHappening()
        {
            foreach (var challenge in ChallengeNetwork.GetChallenges())
            {
                Debug.Log($"Challenge between {challenge.Client1Id} and {challenge.Client2Id}");
            }
        }
        
        [Command("challenge", "Prints the challenges happening")]
        public static void ChallengesHappening(ulong clientId)
        {
            foreach (var challenge in ChallengeNetwork.GetChallenges())
            {
                
                if (challenge.Client1Id == clientId || challenge.Client2Id == clientId)
                {
                    Debug.Log($"Challenge between {challenge.Client1Id} and {challenge.Client2Id}");
                }
            }
        }
    }
}