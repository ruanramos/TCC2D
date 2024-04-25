using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Challenges.Questions
{
    public class QuestionManager : NetworkBehaviour
    {
        private static Questions _questions;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                LoadQuestions();
            }
        }

        private static Questions LoadQuestions()
        {
            var textAsset = Resources.Load<TextAsset>("Questions/Questions");

            if (textAsset == null)
            {
                Debug.LogError("Failed to load file!");
                return null;
            }

            _questions = JsonUtility.FromJson<Questions>(textAsset.text.ToLower());
            
            if (_questions.questions == null)
            {
                Debug.LogError("Failed to parse data!");
            }

            return _questions;
        }

        // Function to get a random question
        public static Question GetRandomQuestion()
        {
            if (_questions == null || _questions.questions.Length == 0)
            {
                print("No questions loaded!");
                return null;
            }

            
            var randomIndex = Random.Range(0, _questions.questions.Length);
            print($"Random index: {randomIndex}");
            print("Random question: " + _questions.questions[randomIndex].query);
            return _questions.questions[randomIndex];
        }

        // Function to check if answer matches
        public static bool CheckAnswer(Question question, string answer)
        {
            // Check if question.answers contains answer
            return ((IList)question.answers).Contains(answer.ToLower());
        }
    }
}