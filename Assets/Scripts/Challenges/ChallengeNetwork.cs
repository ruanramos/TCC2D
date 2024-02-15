using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

namespace Challenges
{
    public class ChallengeNetwork : NetworkBehaviour
    {
        private ulong _client1Id;
        private ulong _client2Id;
        private static HashSet<Challenge> _challenges;

        private void Awake()
        {
            _challenges = new HashSet<Challenge>();
        }

        /*public static ulong CalculateFasterClient(Challenge challenge)
        {
            /*var client1Time = challenge.ClientFinishTimestamps[challenge.Client1Id];
            var client2Time = challenge.ClientFinishTimestamps[challenge.Client2Id];#1#
            //return client1Time < client2Time ? challenge.Client1Id : challenge.Client2Id;
        }*/

        /*public static Challenge CreateChallenge(ulong client1Id, ulong client2Id)
        {
            var existingChallenge = _challenges.FirstOrDefault(challenge =>
                challenge.Client1Id == client1Id && challenge.Client2Id == client2Id ||
                challenge.Client1Id == client2Id && challenge.Client2Id == client1Id
            );

            print($"Existing challenge: {existingChallenge}");

            if (existingChallenge != null && existingChallenge.Client1Id != 0) return existingChallenge;
            
            var challengeData = gameObject.AddComponent<Challenge>();

            _challenges.Add(challengeData);
            print($"Created challenge between {client1Id} and {client2Id}");

            return challengeData;
        }*/

        public static void DestroyChallenge(ulong client1Id, ulong client2Id)
        {
            foreach (var challenge in _challenges.Where(challenge =>
                         challenge.Client1Id == client1Id && challenge.Client2Id == client2Id ||
                         challenge.Client1Id == client2Id && challenge.Client2Id == client1Id))
            {
                _challenges.Remove(challenge);
                break;
            }
        }

        public static HashSet<Challenge> GetChallenges()
        {
            return _challenges;
        }
    }
}