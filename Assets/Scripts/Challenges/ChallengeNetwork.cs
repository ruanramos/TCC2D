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
                Debug.Log($"Challenge between {component.Client1Id} and {component.Client2Id}");
            }
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
                component.Client1Id == client1Id && component.Client2Id == client2Id ||
                component.Client1Id == client2Id && component.Client2Id == client1Id);
        }
    }
}