﻿using Unity.Netcode;
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

        private void Awake()
        {
            print("AAAAAAAAAAAAAAAAAAAAA");
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

            _questions = JsonUtility.FromJson<Questions>(textAsset.text);
            print("++++++++++++++++");
            // print questions
            foreach (var question in _questions.questions)
            {
                print(question.query);
                print(question.answer);
            }

            print("++++++++++++++++");
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
            return _questions.questions[randomIndex];
        }

        // Function to check if answer matches
        public static bool CheckAnswer(Question question, string answer)
        {
            // Check if question.answers contains answer
            return question.answer.Contains(answer);
        }
    }
}