using QFSW.QC;
using UnityEngine;

namespace Console
{
    public class ConsoleCommands
    {
        /*[Command("connectedPlayers", "Prints the list of connected players")]
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
        }*/
        
        
    }
}