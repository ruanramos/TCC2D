using Challenges;
using QFSW.QC;
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
                var component = challenge.GetComponent<Challenge>();
                Debug.Log($"Challenge between {component.Client1Id} and {component.Client2Id}");
            }
        }

        [Command("challenge", "Prints the challenge client is in")]
        public static void ChallengesHappening(ulong clientId)
        {
            foreach (var challenge in ChallengeNetwork.GetChallenges())
            {
                var component = challenge.GetComponent<Challenge>();
                if (component.Client1Id == clientId || component.Client2Id == clientId)
                {
                    Debug.Log($"Challenge between {component.Client1Id} and {component.Client2Id}");
                }
            }
        }

        [Command("addScore", "Add score to player")]
        public static void AddScore(ulong clientId, int n)
        {
            foreach (var playerNetwork in Object.FindObjectsOfType<PlayerNetwork>())
            {
                if (playerNetwork.OwnerClientId != clientId) continue;
                playerNetwork.AddScoreServerRpc(n);
                break;
            }
        }

        [Command("leaveChallenges", "Makes all players isInChallenge = false")]
        public static void LeaveChallenges()
        {
            foreach (var playerNetwork in Object.FindObjectsOfType<PlayerNetwork>())
            {
                playerNetwork.SetIsInChallenge(false);
            }
        }
    }
}