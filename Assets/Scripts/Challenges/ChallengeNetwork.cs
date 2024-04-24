using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static GameConstants;

namespace Challenges
{
    public class ChallengeNetwork : NetworkBehaviour
    {
        private static HashSet<GameObject> _challenges;

        private void Awake()
        {
            _challenges = new HashSet<GameObject>();
        }

        private void Update()
        {
            // Print challenges happening on command
            if (!Input.GetKeyDown(KeyCode.F2)) return;
            foreach (var component in _challenges.Select(challenge => challenge.GetComponent<Challenge>()))
            {
                Debug.Log($"Challenge between {component.Client1Id.Value} and {component.Client2Id.Value}");
            }
        }

        public static GameObject CreateAndSpawnChallenge(ulong client1, ulong client2)
        {
            var challengePrefab = Resources.Load<GameObject>("Prefabs/Challenge");
            var instance = Instantiate(challengePrefab, Vector3.zero, Quaternion.identity);
            AddNewChallenge(instance);
            var challengeComponent = instance.GetComponent<Challenge>();
            if (!instance.GetComponent<NetworkObject>().IsSpawned)
            {
                // Need to set the type of challenge here
                challengeComponent.Type.Value = ChallengeType.TypingChallenge;
                
                instance.GetComponent<NetworkObject>().Spawn();
                challengeComponent.Client1Id.Value = client1;
                challengeComponent.Client2Id.Value = client2;
                var connectedClients = NetworkManager.Singleton
                    .ConnectedClients;
                challengeComponent.Client1Name.Value = connectedClients[client1]
                    .PlayerObject.gameObject.GetComponent<PlayerNetwork>().GetPlayerName();
                challengeComponent.Client2Name.Value = connectedClients[client2]
                    .PlayerObject.gameObject.GetComponent<PlayerNetwork>().GetPlayerName();
            }

            print(
                $"<color=#00FF00>Spawned challenge network object for players {challengeComponent.Client1Name.Value} and {challengeComponent.Client2Name.Value} at time {NetworkManager.Singleton.ServerTime.Time}</color>");
            return instance;
        }

        public static IEnumerator StartChallenge(GameObject challenge, GameObject player1, GameObject player2)
        {
            yield return new WaitForSeconds(ChallengeStartDelayInSeconds);

            var player1NetworkBehaviour = player1.GetComponent<NetworkBehaviour>();
            var player2NetworkBehaviour = player2.GetComponent<NetworkBehaviour>();
            var player1Network = player1.GetComponent<PlayerNetwork>();
            var player2Network = player2.GetComponent<PlayerNetwork>();
            var player1Id = player1NetworkBehaviour.OwnerClientId;
            var player2Id = player2NetworkBehaviour.OwnerClientId;

            var challengeComponent = challenge.GetComponent<Challenge>();

            yield return new WaitForSeconds(ChallengeTimeoutLimitInSeconds);

            var winnerId = challengeComponent.DecideWinner();
            var loserId = winnerId == player2Id
                ? player1Id
                : player2Id;

            // Set text to show winner
            challengeComponent.DisplayResultsServerRpc(player1Id, player2Id, winnerId);

            yield return new WaitForSeconds(ChallengeWinnerTime);

            player1Network.SetIsInChallenge(false);
            player2Network.SetIsInChallenge(false);

            if (winnerId == 0)
            {
                print(
                    $"Finishing challenge between players {player1Id}" +
                    $" and {player2Id}. No winner");

                // Both players lose a health
                player1Network.RemoveLives(1);
                player2Network.RemoveLives(1);
                print($"Player {player1Id} and {player2Id} lost a life");

                yield break;
            }

            var loser = NetworkManager.Singleton.ConnectedClients[loserId].PlayerObject.gameObject;
            var winner = NetworkManager.Singleton.ConnectedClients[winnerId].PlayerObject.gameObject;

            print(
                $"Finishing challenge simulation between players {player1Id}" +
                $" and {player2Id}. Player {winnerId} wins");
            loser.gameObject.GetComponent<PlayerNetwork>().RemoveLives(1);
            if (winner.gameObject.GetComponent<PlayerNetwork>().GetLives() < MaxLives)
            {
                winner.gameObject.GetComponent<PlayerNetwork>().AddLives(1);
                yield break;
            }

            print($"Could not increment player {winnerId} lives because it is already at max lives");
        }

        public static HashSet<GameObject> GetChallenges()
        {
            return _challenges;
        }

        public static void AddNewChallenge(GameObject newChallenge)
        {
            _challenges.Add(newChallenge);
        }

        public static bool ChallengeExists(ulong client1Id, ulong client2Id)
        {
            return _challenges.Select(challenge => challenge.GetComponent<Challenge>()).Any(component =>
                component.Client1Id.Value == client1Id && component.Client2Id.Value == client2Id ||
                component.Client1Id.Value == client2Id && component.Client2Id.Value == client1Id);
        }

        public static Challenge IsInChallenge(ulong clientId)
        {
            return _challenges.Select(challenge => challenge.GetComponent<Challenge>()).FirstOrDefault(component =>
                component.Client1Id.Value == clientId || component.Client2Id.Value == clientId);
        }

        public static void RemoveChallenge(GameObject challenge)
        {
            _challenges.Remove(challenge);
        }

        public static Challenge GetChallenge(ulong client1Id, ulong client2Id)
        {
            return _challenges.Select(challenge => challenge.GetComponent<Challenge>()).FirstOrDefault(component =>
                component.Client1Id.Value == client1Id && component.Client2Id.Value == client2Id ||
                component.Client1Id.Value == client2Id && component.Client2Id.Value == client1Id);
        }
    }
}