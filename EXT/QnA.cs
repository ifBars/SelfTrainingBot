using OpenNLP.Tools.Ling;
using OpenNLP.Tools.Util;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SelfTrainingBot.NLP
{
    public class QnA
    {

        private static Dictionary<string, KnowledgeEntry> knowledgeBase = new Dictionary<string, KnowledgeEntry>();

        public void Train(double score, string sentence, string generatedQuestion, string generatedAnswer)
        {
            // Check if inputs are valid
            if (string.IsNullOrEmpty(sentence) || string.IsNullOrEmpty(generatedQuestion) || string.IsNullOrEmpty(generatedAnswer))
            {
                return;
            }

            // Calculate the weight of the score (0.0 <= weight <= 1.0)
            double weight = Math.Min(Math.Max(score, 0.0), 1.0);

            // Split the sentence into words
            string[] words = sentence.Split(' ');

            // Determine the subject and verb of the sentence
            string subject = GetSubject(words);
            string[] verbAndTense = GetVerbAndTense(words);
            string verb = verbAndTense[0];
            string tense = verbAndTense[1];

            // Update the knowledge base with the generated question and answer
            string key = $"{subject}_{verb}_{tense}";
            if (knowledgeBase.ContainsKey(key))
            {
                KnowledgeEntry entry = knowledgeBase[key];
                entry.GeneratedQuestions.Add(generatedQuestion);
                entry.GeneratedAnswers.Add(generatedAnswer);
                entry.Scores.Add(weight);
            }
            else
            {
                KnowledgeEntry entry = new KnowledgeEntry();
                entry.Subject = subject;
                entry.Verb = verb;
                entry.Tense = tense;
                entry.GeneratedQuestions.Add(generatedQuestion);
                entry.GeneratedAnswers.Add(generatedAnswer);
                entry.Scores.Add(weight);
                knowledgeBase.Add(key, entry);
            }
        }

        public static string ConvertToQuestion(string sentence)
        {
            // check if sentence is a valid question
            string[] questionWords = { "Who", "What", "Where", "When", "Why", "Whose", "Whom", "Whether", "How", "Which", "Is", "Was", "Are", "Were", "Has", "Have", "Had" };
            string prefer = null;
            int maxScore = -1;

            foreach (string word in questionWords)
            {
                if (sentence.StartsWith(word + " ", StringComparison.OrdinalIgnoreCase) || sentence.StartsWith(word + "'s ", StringComparison.OrdinalIgnoreCase))
                {
                    int score = word.Length;

                    if (score > maxScore)
                    {
                        prefer = word;
                        maxScore = score;
                    }
                }
            }

            // extract the main idea from the sentence
            string mainIdea = ExtractMainIdea(sentence);

            // generate the question based on the main idea
            string question = null;
            if (mainIdea != null)
            {
                // determine the verb tense of the sentence
                string[] words = mainIdea.Split(' ');

                // check if the first word is a conjunction and skip it
                int startIndex = 0;
                if (IsConjunction(words[0]))
                {
                    startIndex = 1;
                }

                string[] verbAndTense = GetVerbAndTense(words);
                string verb = verbAndTense[0];
                string tense = verbAndTense[1];

                // generate the question based on the tense
                switch (tense)
                {
                    case "present":
                        try
                        {
                            string presentVerb = GetPresentTenseVerb(words);
                            if (presentVerb == null)
                            {
                                return null;
                            }
                            question = $"{prefer} {presentVerb[0]}{presentVerb.Substring(1)} {GetRestOfSentence(words, GetSubjectIndex(words) + 1)}?";
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Exception: {e.Message}");
                            return null;
                        }
                        break;
                    case "past":
                        question = $"{prefer} {GetPastTenseVerb(words)[0]}{GetPastTenseVerb(words).Substring(1)} {GetRestOfSentence(words, GetSubjectIndex(words) + 1)}?";
                        break;
                    case "future":
                        question = $"{prefer} will {GetRestOfSentence(words, 0)}?";
                        break;
                    default:
                        question = $"{prefer} {GetRestOfSentence(words, 0)}?";
                        break;
                }
                question = question.TrimEnd('.', '!', '?');
            }

            return question;
        }

        static string ExtractMainIdea(string sentence)
        {
            // Handle null or empty input
            if (string.IsNullOrEmpty(sentence))
            {
                return null;
            }

            // Remove leading/trailing spaces and punctuation
            sentence = Regex.Replace(sentence.Trim(), @"[\p{P}\p{S}]+$", "");

            // Replace multiple spaces with a single space
            sentence = Regex.Replace(sentence, @"\s+", " ");

            // Split the sentence into clauses
            string[] clauses = sentence.Split(new string[] { "and", "but", "or", "because" }, StringSplitOptions.RemoveEmptyEntries);

            // Find the main idea by looking for the longest clause
            string mainIdea = null;
            int maxLength = 0;
            foreach (string clause in clauses)
            {
                string trimmedClause = clause.Trim();
                if (trimmedClause.Length > maxLength)
                {
                    mainIdea = trimmedClause;
                    maxLength = trimmedClause.Length;
                }
            }
            return mainIdea;
        }

        public static string ExtractAnswer(string sentence, string generatedQuestion)
        {
            // Handle null or empty inputs
            if (string.IsNullOrEmpty(sentence) || string.IsNullOrEmpty(generatedQuestion))
            {
                return null;
            }

            // Handle different cases of question words
            if (generatedQuestion.StartsWith("What", StringComparison.OrdinalIgnoreCase))
            {
                generatedQuestion = generatedQuestion.Replace("What", "When", StringComparison.OrdinalIgnoreCase);
            }
            else if (generatedQuestion.StartsWith("Who", StringComparison.OrdinalIgnoreCase))
            {
                generatedQuestion = generatedQuestion.Replace("who", "When", StringComparison.OrdinalIgnoreCase);
            }
            else if (generatedQuestion.StartsWith("Where", StringComparison.OrdinalIgnoreCase))
            {
                generatedQuestion = generatedQuestion.Replace("where", "When", StringComparison.OrdinalIgnoreCase);
            }
            else if (generatedQuestion.StartsWith("Why", StringComparison.OrdinalIgnoreCase))
            {
                generatedQuestion = generatedQuestion.Replace("why", "Because", StringComparison.OrdinalIgnoreCase);
            }

            // Split the sentence into words
            string[] words = sentence.Split(' ');

            // Determine the subject and verb of the sentence
            string subject = GetSubject(words);
            string[] verbAndTense = GetVerbAndTense(words);
            string verb = verbAndTense[0];
            string tense = verbAndTense[1];

            // Check if verb tense matches generated question
            string generatedTense = GetVerbTense(generatedQuestion);
            if (generatedTense != tense)
            {
                return null;
            }

            // Extract the answer based on the subject and verb
            string answer = ExtractAnswerBasedOnSubjectAndVerb(subject, verb, tense, words);

            return answer;
        }

        private static string ExtractAnswerBasedOnSubjectAndVerb(string subject, string verb, string tense, string[] words)
        {
            if (subject != null && verb != null)
            {
                string[] compoundVerbs = verb.Split(' ');
                if (compoundVerbs.Length > 1)
                {
                    verb = compoundVerbs[0];
                }

                switch (verb)
                {
                    case "is":
                    case "was":
                        return ExtractIsOrWasAnswer(subject, words, verb);
                    case "are":
                    case "were":
                        return ExtractAreOrWereAnswer(subject, words, verb);
                    case "has":
                    case "have":
                    case "had":
                        return ExtractHasHaveHadAnswer(tense, verb, words);
                    default:
                        return ExtractDefaultAnswer(subject, verb, words);
                }
            }
            else
            {
                return ExtractDefaultAnswer(null, null, words);
            }
        }

        public static string ExtractHasHaveHadAnswer(string tense, string verb, string[] nouns)
        {
            string noun1 = nouns[0];
            string noun2 = nouns[1];
            string noun3 = nouns[2];

            string verbForm = "";
            switch (tense.ToLower())
            {
                case "present":
                    verbForm = verb.EndsWith("s") ? "have" : "has";
                    break;
                case "past":
                    verbForm = "had";
                    break;
                case "future":
                    verbForm = "will have";
                    break;
                default:
                    return "";
            }

            string sentence = $"The {noun1} {verb} {verbForm} {noun2} and {noun3}.";

            return sentence;
        }

        private static string ExtractIsOrWasAnswer(string subject, string[] words, string verb)
        {
            // Check if subject is a pronoun
            if (IsPronoun(subject))
            {
                // Extract the noun after "is" or "was"
                int index = Array.IndexOf(words, verb);
                if (index >= 0 && index < words.Length - 1)
                {
                    string noun = words[index + 1];
                    return ReplacePronoun(subject, noun);
                }
            }
            else
            {
                // Extract the predicate after the subject
                int index = Array.IndexOf(words, subject);
                if (index >= 0 && index < words.Length - 1)
                {
                    string predicate = string.Join(" ", words, index + 1, words.Length - index - 1);
                    return predicate;
                }
            }
            return null;
        }

        private static string ExtractAreOrWereAnswer(string subject, string[] words, string verb)
        {
            // Check if subject is a pronoun
            if (IsPronoun(subject))
            {
                // Extract the noun after "are" or "were"
                int index = Array.IndexOf(words, verb);
                if (index >= 0 && index < words.Length - 1)
                {
                    string noun = words[index + 1];
                    return ReplacePronoun(subject, noun);
                }
            }
            else
            {
                // Extract the predicate after the subject
                int index = Array.IndexOf(words, subject);
                if (index >= 0 && index < words.Length - 1)
                {
                    string predicate = string.Join(" ", words, index + 1, words.Length - index - 1);
                    return predicate;
                }
            }
            return null;
        }

        private static string ExtractDefaultAnswer(string subject, string verb, string[] words)
        {
            // check if subject is a pronoun
            if (IsPronoun(subject))
            {
                // extract the noun after the verb
                int index = Array.IndexOf(words, verb);
                if (index >= 0 && index < words.Length - 1)
                {
                    string noun = words[index + 1];
                    return ReplacePronoun(subject, noun);
                }
            }
            else
            {
                // extract the predicate after the subject
                int index = Array.IndexOf(words, subject);
                if (index >= 0 && index < words.Length - 1)
                {
                    string predicate = string.Join(" ", words, index + 1, words.Length - index - 1);
                    return predicate;
                }
            }
            return null;
        }

        // similar methods for ExtractWasAnswer, ExtractAreAnswer, ExtractWereAnswer, ExtractHasAnswer, ExtractHaveAnswer, ExtractHadAnswer, and ExtractDefaultAnswer

        private static string ReplacePronoun(string pronoun, string noun)
        {
            switch (pronoun)
            {
                case "I":
                    return "You are " + noun;
                case "you":
                    return "I am " + noun;
                case "he":
                    return "He is " + noun;
                case "she":
                    return "She is " + noun;
                case "it":
                    return "It is " + noun;
                case "we":
                    return "We are " + noun;
                case "they":
                    return "They are " + noun;
                default:
                    return pronoun + " is " + noun;
            }
        }
        private static bool IsPronoun(string word)
        {
            switch (word)
            {
                case "I":
                case "you":
                case "he":
                case "she":
                case "it":
                case "we":
                case "they":
                    return true;
                default:
                    return false;
            }
        }

        public static string GetSubject(string[] words)
        {
            // look for the first noun in the sentence
            for (int i = 0; i < words.Length; i++)
            {
                if (IsNoun(words[i]))
                {
                    return words[i];
                }
            }

            // if no noun is found, return null
            return null;
        }

        public static int GetSubjectIndex(string[] words)
        {
            // look for the subject in the sentence
            string subject = GetSubject(words);
            if (subject != null)
            {
                return Array.IndexOf(words, subject);
            }

            // if no subject is found, return -1
            return -1;
        }

        public static string GetVerb(string[] words)
        {

            foreach (string word in words)
            {
                if (IsVerb(word))
                {
                    return word;
                }
            }

            // if no verb is found, return null
            return null;
        }


        public static string GetPresentTenseVerb(string[] words)
        {
            // look for the first verb in the sentence
            for (int i = 0; i < words.Length; i++)
            {
                if (IsVerb(words[i]))
                {
                    // check if the verb is in the present tense
                    if (!words[i].EndsWith("ed", StringComparison.OrdinalIgnoreCase) && !words[i].EndsWith("ing", StringComparison.OrdinalIgnoreCase))
                    {
                        return words[i];
                    }

                    // if the verb is in the past tense, try to convert it to the present tense
                    string baseVerb = GetBaseVerb(words[i]);
                    if (baseVerb != null)
                    {
                        return baseVerb;
                    }

                    // otherwise, throw an exception
                    throw new Exception("Unable to determine present tense verb.");
                }
            }

            // if no verb is found, throw an exception
            throw new Exception("Unable to find verb in sentence.");
        }

        public static string GetBaseVerb(string verb)
        {
            // check if the verb ends with "ing"
            if (verb.EndsWith("ing", StringComparison.OrdinalIgnoreCase))
            {
                // remove the "ing" suffix to get the base form of the verb
                string baseVerb = verb.Substring(0, verb.Length - 3);

                // check if the base form is a valid verb
                if (IsVerb(baseVerb))
                {
                    return baseVerb;
                }

                // check if the base form with "e" suffix is a valid verb
                if (verb.EndsWith("ying", StringComparison.OrdinalIgnoreCase))
                {
                    baseVerb = verb.Substring(0, verb.Length - 4) + "e";
                    if (IsVerb(baseVerb))
                    {
                        return baseVerb;
                    }
                }
            }
            // check if the verb ends with "ed"
            else if (verb.EndsWith("ed", StringComparison.OrdinalIgnoreCase))
            {
                // remove the "ed" suffix to get the base form of the verb
                string baseVerb = verb.Substring(0, verb.Length - 2);

                // check if the base form is a valid verb
                if (IsVerb(baseVerb))
                {
                    return baseVerb;
                }

                // check if the base form with "e" suffix is a valid verb
                if (verb.EndsWith("ied", StringComparison.OrdinalIgnoreCase))
                {
                    baseVerb = verb.Substring(0, verb.Length - 3) + "y";
                    if (IsVerb(baseVerb))
                    {
                        return baseVerb;
                    }
                }

                // check if the base form with "d" suffix is a valid verb
                if (verb.EndsWith("dd", StringComparison.OrdinalIgnoreCase) || verb.EndsWith("ed", StringComparison.OrdinalIgnoreCase))
                {
                    baseVerb = verb.Substring(0, verb.Length - 1);
                    if (IsVerb(baseVerb))
                    {
                        return baseVerb;
                    }
                }
            }

            // if no base verb is found, return null
            return null;
        }


        public static string GetPastTenseVerb(string[] words)
        {
            // look for the last verb in the sentence
            for (int i = words.Length - 1; i >= 0; i--)
            {
                if (IsVerb(words[i]))
                {
                    // check if the verb is in the past tense
                    if (words[i].EndsWith("ed", StringComparison.OrdinalIgnoreCase))
                    {
                        return words[i];
                    }

                    // if the verb is in the present tense, try to convert it to the past tense
                    if (words[i].EndsWith("ing", StringComparison.OrdinalIgnoreCase))
                    {
                        string baseVerb = words[i].Substring(0, words[i].Length - 3);
                        return baseVerb + "ed";
                    }

                    // otherwise, return null
                    return null;
                }
            }

            // if no verb is found, return null
            return null;
        }


        public static string GetVerbTense(string sentence)
        {
            // split the sentence into words
            string[] words = sentence.Split(' ');

            // look for the last verb in the sentence
            for (int i = words.Length - 1; i >= 0; i--)
            {
                if (IsVerb(words[i]))
                {
                    // check if the verb is in the present tense
                    if (words[i].EndsWith("ing", StringComparison.OrdinalIgnoreCase))
                    {
                        return "present";
                    }

                    // check if the verb is in the past tense
                    if (words[i].EndsWith("ed", StringComparison.OrdinalIgnoreCase))
                    {
                        return "past";
                    }

                    // otherwise, assume the verb is in the future tense
                    return "future";
                }
            }

            // if no verb is found, return null
            return null;
        }

        public static string[] GetVerbAndTense(string[] words)
        {
            string[] result = new string[2];

            // look for the verb in the sentence
            string verb = GetVerb(words);
            result[0] = verb;

            // check the tense of the verb
            if (verb != null)
            {
                // check if the verb is in the present tense
                if (verb.EndsWith("ing", StringComparison.OrdinalIgnoreCase))
                {
                    result[1] = "present";
                }
                // check if the verb is in the past tense
                else if (verb.EndsWith("ed", StringComparison.OrdinalIgnoreCase))
                {
                    result[1] = "past";
                }
                // otherwise, assume the verb is in the future tense
                else
                {
                    result[1] = "future";
                }
            }

            return result;
        }

        public static string GetRestOfSentence(string[] words, int startIndex)
        {
            // combine the remaining words into a single string
            StringBuilder sb = new StringBuilder();
            for (int i = startIndex; i < words.Length; i++)
            {
                sb.Append(words[i]);
                if (i < words.Length - 1)
                {
                    sb.Append(" ");
                }
            }
            sb.Append(".");
            return sb.ToString();
        }




        public static bool IsNoun(string word)
        {
            // Check if the word is capitalized (assuming that capitalized words are proper nouns)
            if (word.Length > 0 && char.IsUpper(word[0]))
            {
                return true;
            }

            // Check if the word is a plural noun (assuming that plural nouns end in "s" or "es")
            if (word.EndsWith("s") || word.EndsWith("es"))
            {
                string singular = word.Substring(0, word.Length - 1);

                // Check if the singular form of the word is a valid noun (assuming that singular nouns are capitalized)
                return singular.Length > 0 && char.IsUpper(singular[0]);
            }

            return false;
        }
        public static bool IsConjunction(string word)
        {
            string[] conjunctions = { "and", "but", "or", "yet", "so", "for", "nor" };
            return conjunctions.Contains(word.ToLower());
        }

        public static bool IsVerb(string word)
        {
            // Check if the word is a gerund (assuming that gerunds end in "ing")
            if (word.EndsWith("ing", StringComparison.OrdinalIgnoreCase))
            {
                string baseForm = word.Substring(0, word.Length - 3);

                // Check if the base form of the word is a valid verb (assuming that base forms of verbs end in "e" or "t")
                return baseForm.EndsWith("e", StringComparison.OrdinalIgnoreCase) || baseForm.EndsWith("t", StringComparison.OrdinalIgnoreCase);
            }

            // Check if the word is a past participle (assuming that past participles end in "ed")
            if (word.EndsWith("ed", StringComparison.OrdinalIgnoreCase))
            {
                string baseForm = word.Substring(0, word.Length - 2);

                // Check if the base form of the word is a valid verb (assuming that base forms of verbs end in "e" or "t")
                return baseForm.EndsWith("e", StringComparison.OrdinalIgnoreCase) || baseForm.EndsWith("t", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }



    }
}
