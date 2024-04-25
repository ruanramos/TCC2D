using System;

namespace Challenges.Questions
{
    [Serializable]
    public class Question
    {
        /*
         * This class stores the question data loaded from the JSON file
         *
         * A question have a question string and a list of right answers
         */
        public string query;
        public string[] answers;
    }
}