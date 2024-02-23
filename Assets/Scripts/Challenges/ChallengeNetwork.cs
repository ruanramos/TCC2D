using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

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