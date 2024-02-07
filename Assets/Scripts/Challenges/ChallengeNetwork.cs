using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

namespace Challenges
{
    public class ChallengeNetwork : NetworkBehaviour
    {
        private ulong _client1Id;
        private ulong _client2Id;
        private static List<ChallengeData> _challenges;

        private void Awake()
        {
            _challenges = new List<ChallengeData>();
        }


        /*[ClientRpc]
    public void declareWinnerClientRpc(ClientRpcSendParams clientRpcSendParams)
    {
    }*/

        public static ulong CalculateFasterClient(ulong client1Id, ulong client2Id, double client1Time,
            double client2Time)
        {
            return client1Time < client2Time ? client1Id : client2Id;
        }

        public static void CreateChallenge(ulong client1Id, ulong client2Id)
        {
            var challengeData = new ChallengeData(client1Id, client2Id);
            if (_challenges.Any(challenge => challenge.Equals(challengeData))) return;
            _challenges.Add(challengeData);
            print($"Created challenge between {client1Id} and {client2Id}");
        }

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

        public static List<ChallengeData> GetChallenges()
        {
            return _challenges;
        }
    }
}