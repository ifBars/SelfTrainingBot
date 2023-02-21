using System.Text;
using System.Text.RegularExpressions;
using SelfTrainingBot.EXT;

namespace SelfTrainingBot.NLP
{
    public class QnA
    {

        private static Dictionary<string, KnowledgeEntry> knowledgeBase = new Dictionary<string, KnowledgeEntry>();

        public static void Train(double score, string sentence, string generatedQuestion, string generatedAnswer)
        {
            // Check if inputs are valid
            if (string.IsNullOrEmpty(sentence) || string.IsNullOrEmpty(generatedQuestion) || string.IsNullOrEmpty(generatedAnswer))
            {
                Console.WriteLine("Debug: Entry is Null or Empty");
                return;
            }

            Console.WriteLine("Debug: Training dictionary initializing");
            // Calculate the weight of the score (0.0 <= weight <= 1.0)
            double weight = Math.Min(Math.Max(score, 0.0), 1.0);

            Console.WriteLine("Debug: Splitting words");
            // Split the sentence into words
            string[] words = sentence.Split(' ');

            Console.WriteLine("Debug: Grabbing subject and verb");
            // Determine the subject and verb of the sentence
            string subject = Grammar.GetSubject(words);
            string[] verbAndTense = Grammar.GetVerbAndTense(words);
            string verb = verbAndTense[0];
            string tense = verbAndTense[1];

            Console.WriteLine("Debug: Updating knowledgebase");
            // Update the knowledge base with the generated question and answer
            string key = $"{subject}_{verb}_{tense}";
            if (knowledgeBase.ContainsKey(key))
            {
                Console.WriteLine("Debug: Knowledgebase contains key");
                KnowledgeEntry entry = knowledgeBase[key];
                entry.GeneratedQuestions.Add(generatedQuestion);
                entry.GeneratedAnswers.Add(generatedAnswer);
                entry.Scores.Add(weight);
            }
            else
            {
                Console.WriteLine("Debug: Knowledgebase does not contain key");
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
                if (Grammar.IsConjunction(words[0]))
                {
                    startIndex = 1;
                }

                string[] verbAndTense = Grammar.GetVerbAndTense(words);
                string verb = verbAndTense[0];
                string tense = verbAndTense[1];

                // generate the question based on the tense
                switch (tense)
                {
                    case "present":
                        try
                        {
                            string presentVerb = Grammar.GetPresentTenseVerb(words);
                            if (presentVerb == null)
                            {
                                return null;
                            }
                            question = $"{prefer} {presentVerb[0]}{presentVerb.Substring(1)} {GetRestOfSentence(words, Grammar.GetSubjectIndex(words) + 1)}?";
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Exception: {e.Message}");
                            return null;
                        }
                        break;
                    case "past":
                        question = $"{prefer} {Grammar.GetPastTenseVerb(words)[0]}{Grammar.GetPastTenseVerb(words).Substring(1)} {GetRestOfSentence(words, Grammar.GetSubjectIndex(words) + 1)}?";
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
            string subject = Grammar.GetSubject(words);
            string[] verbAndTense = Grammar.GetVerbAndTense(words);
            string verb = verbAndTense[0];
            string tense = verbAndTense[1];

            // Check if verb tense matches generated question
            string generatedTense = Grammar.GetVerbTense(generatedQuestion);
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
            if (Grammar.IsPronoun(subject))
            {
                // Extract the noun after "is" or "was"
                int index = Array.IndexOf(words, verb);
                if (index >= 0 && index < words.Length - 1)
                {
                    string noun = words[index + 1];
                    return Grammar.ReplacePronoun(subject, noun);
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
            if (Grammar.IsPronoun(subject))
            {
                // Extract the noun after "are" or "were"
                int index = Array.IndexOf(words, verb);
                if (index >= 0 && index < words.Length - 1)
                {
                    string noun = words[index + 1];
                    return Grammar.ReplacePronoun(subject, noun);
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
            if (Grammar.IsPronoun(subject))
            {
                // extract the noun after the verb
                int index = Array.IndexOf(words, verb);
                if (index >= 0 && index < words.Length - 1)
                {
                    string noun = words[index + 1];
                    return Grammar.ReplacePronoun(subject, noun);
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

    }
}
