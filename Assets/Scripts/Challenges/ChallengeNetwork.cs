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
            if (Input.GetKeyDown(KeyCode.Space) && NetworkManager.LocalClient != null)
            {
                SendKeyboardTimestampToServerServerRpc(NetworkManager.Singleton.ServerTime.Time,
                    Input.inputString);
            }

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
            if (!instance.GetComponent<NetworkObject>().IsSpawned)
            {
                instance.GetComponent<NetworkObject>().Spawn();
                instance.GetComponent<Challenge>().Client1Id.Value = client1;
                instance.GetComponent<Challenge>().Client2Id.Value = client2;
            }

            print(
                $"<color=#00FF00>Spawned challenge network object for players {client1} and {client2} at time {NetworkManager.Singleton.ServerTime.Time}</color>");
            return instance;
        }

        /*public static ulong CalculateFasterClient(Challenge challenge)
        {
            /*var client1Time = challenge.ClientFinishTimestamps[challenge.Client1Id];
            var client2Time = challenge.ClientFinishTimestamps[challenge.Client2Id];#1#
            //return client1Time < client2Time ? challenge.Client1Id : challenge.Client2Id;
        }*/

        [ServerRpc(RequireOwnership = false)]
        private void SendKeyboardTimestampToServerServerRpc(double time, string key)
        {
            print(
                $"Received {key} press from client {OwnerClientId}\n" +
                $"timestamp: {time}  --- Server time: {NetworkManager.Singleton.ServerTime.Time}");
        }

        public static IEnumerator SimulateChallenge(GameObject player1, GameObject player2)
        {
            // Check if simulation already exists
            
            
            var player1NetworkBehaviour = player1.GetComponent<NetworkBehaviour>();
            var player2NetworkBehaviour = player2.GetComponent<NetworkBehaviour>();
            var player1Network = player1.GetComponent<PlayerNetwork>();
            var player2Network = player2.GetComponent<PlayerNetwork>();
            var player1Id = player1NetworkBehaviour.OwnerClientId;
            var player2Id = player2NetworkBehaviour.OwnerClientId;

            yield return new WaitForSeconds(ChallengeSimulationTimeInSeconds);

            var winner = Random.Range(0, 2) == 0
                ? player1
                : player2;
            var loser = winner == player2
                ? player1
                : player2;

            player1Network.SetIsInChallenge(false);
            player2Network.SetIsInChallenge(false);

            var loserId = loser.GetComponent<NetworkBehaviour>().OwnerClientId;
            var winnerId = winner.GetComponent<NetworkBehaviour>().OwnerClientId;
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