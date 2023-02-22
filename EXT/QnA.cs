using System.Diagnostics.Metrics;
using System.Text;
using System.Text.RegularExpressions;
using SelfTrainingBot.EXT;

namespace SelfTrainingBot.NLP
{
    public class QnA
    {

        public static void Train(double score, string[] words, string generatedQuestion, string generatedAnswer, ML ml)
        {
            // Check if inputs are valid
            if (words == null || words.Length == 0 || string.IsNullOrEmpty(generatedQuestion) || string.IsNullOrEmpty(generatedAnswer))
            {
                Console.WriteLine("Debug: Entry is Null or Empty");
                return;
            }

            Console.WriteLine("Debug: Training dictionary initializing");
            // Calculate the weight of the score (0.0 <= weight <= 1.0)
            double weight = Math.Min(Math.Max(score, 0.0), 1.0);

            Console.WriteLine("Debug: Splitting words");
            // Determine the subject and verb of the sentence
            string subject = Grammar.GetSubject(words);
            string[] verbAndTense = Grammar.GetVerbAndTense(words);
            string verb = verbAndTense[0];
            string tense = verbAndTense[1];

            Console.WriteLine("Debug: Updating knowledgebase");
            // Update the knowledge base with the generated question and answer
            string key = $"{subject}_{verb}_{tense}";
            if (KnowledgeEntry.knowledgeBase.ContainsKey(key))
            {
                Console.WriteLine("Debug: Knowledgebase contains key");
                KnowledgeEntry entry = KnowledgeEntry.knowledgeBase[key];
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
                KnowledgeEntry.knowledgeBase.Add(key, entry);
            }

            // Train the ML class on the knowledge base
            Console.WriteLine("Debug: Training ML");
            List<string> inputData = new List<string>();
            List<string> outputData = new List<string>();

            foreach (KeyValuePair<string, KnowledgeEntry> kvp in KnowledgeEntry.knowledgeBase)
            {
                inputData.AddRange(kvp.Value.GeneratedAnswers);
                outputData.AddRange(kvp.Value.GeneratedQuestions);
            }

            ml.TrainModel(inputData.ToArray(), outputData.ToArray());

            // Use the ML model to generate better questions
            Console.WriteLine("Debug: Generating questions with ML");
            string[] inputDataForML = new string[] { $"{subject} {verb} {tense}" };
            string[] generatedQuestions = ml.AnalyzeData(inputDataForML);

            foreach (string question in generatedQuestions)
            {
                Console.WriteLine("Debug: ML Generated Questions - " + question);
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

            // Split the sentence into clauses using regular expressions
            string[] clauses = Regex.Split(sentence, @"\b(and|but|or|because)\b", RegexOptions.IgnoreCase);

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

            // If the main idea is a question, extract the answer
            if (mainIdea.EndsWith("?"))
            {
                string generatedQuestion = mainIdea.TrimEnd('?');
                mainIdea = ExtractAnswer(sentence, generatedQuestion);
            }

            return mainIdea;
        }

        public static string ExtractAnswer(string sentence, string generatedQuestion)
        {
            // Handle null or empty inputs
            if (string.IsNullOrEmpty(sentence))
            {
                Console.WriteLine("Debug: Sentence is null");
                return null;
            }
            else if (string.IsNullOrEmpty(generatedQuestion))
            {
                Console.WriteLine("Debug: Question is null");
                return null;
            }

            Console.WriteLine("Debug: Mapping Dictionary");
            // Map question words to answer words
            Dictionary<string, string> questionWordToAnswerWord = new Dictionary<string, string>
            {
                { "what", "it" },
                { "who", "they" },
                { "where", "there" },
                { "why", "because" },
                { "when", "then" },
                { "how", "like this" },
                { "which", "that one" },
                { "whose", "theirs" },
                { "whom", "them" }
            };

            // Replace the question word in the generated question with its corresponding answer word
            string[] generatedQuestionWords = generatedQuestion.Split(' ');
            string firstWord = generatedQuestionWords[0].ToLower();
            if (questionWordToAnswerWord.ContainsKey(firstWord))
            {
                generatedQuestionWords[0] = questionWordToAnswerWord[firstWord];
                generatedQuestion = string.Join(" ", generatedQuestionWords);
            }

            Console.WriteLine("Debug: Splitting sentence");
            // Split the sentence into words
            string[] words = sentence.Split(' ');

            // Determine the subject and verb of the sentence
            string subject = Grammar.GetSubject(words);
            string[] verbAndTense = Grammar.GetVerbAndTense(words);
            string verb = verbAndTense[0];
            string tense = verbAndTense[1];
            Console.WriteLine($"Debug: Subject - {subject}");
            Console.WriteLine($"Debug: Verb - {verb}");
            Console.WriteLine($"Debug: Tense - {tense}");

            Console.WriteLine("Debug: Checking if verb matches question");

            Console.WriteLine("Debug: Extracting answer");
            // Extract the answer based on the subject and verb
            string answer = ExtractAnswerBasedOnSubjectAndVerb(subject, verb, tense, words);

            Console.WriteLine("Debug: Answer being sent: " + answer);
            return answer;
        }
        private static string ExtractAnswerBasedOnSubjectAndVerb(string subject, string verb, string tense, string[] words)
        {
            if (subject == null || verb == null)
            {
                return ExtractDefaultAnswer(subject, verb, words);
            }

            // Remove any punctuation from the subject
            subject = Regex.Replace(subject, @"[\p{P}\p{S}]", "");

            // Remove any leading or trailing whitespace from the subject
            subject = subject.Trim();

            // Remove any articles from the subject (e.g. "a", "an", "the")
            var articles = new[] { "a", "an", "the" };
            subject = string.Join(" ", subject.Split().Where(word => !articles.Contains(word)));

            string[] compoundVerbs = verb.Split(' ');
            if (compoundVerbs.Length > 1)
            {
                verb = compoundVerbs[0];
            }

            string firstWord = words[0];

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
                case "do":
                case "does":
                case "did":
                    if (firstWord == "yes" || firstWord == "no")
                    {
                        return firstWord;
                    }
                    else
                    {
                        return ExtractDefaultAnswer(subject, verb, words);
                    }
                default:
                    return ExtractDefaultAnswer(subject, verb, words);
            }
        }

        public static string ExtractHasHaveHadAnswer(string tense, string verb, string[] nouns)
        {
            if (nouns == null || nouns.Length < 3)
            {
                return "Error: Invalid input";
            }

            string noun1 = nouns[0];
            string noun2 = nouns[1];
            string noun3 = nouns[2];

            StringBuilder sentenceBuilder = new StringBuilder();
            sentenceBuilder.Append("The ");
            sentenceBuilder.Append(noun1);
            sentenceBuilder.Append(" ");
            sentenceBuilder.Append(verb);

            switch (tense.ToLower())
            {
                case "present":
                    sentenceBuilder.Append(verb.EndsWith("s") ? " have " : " has ");
                    break;
                case "past":
                    sentenceBuilder.Append(" had ");
                    break;
                case "future":
                    sentenceBuilder.Append(" will have ");
                    break;
                default:
                    return "Error: Invalid tense";
            }

            sentenceBuilder.Append(noun2);
            sentenceBuilder.Append(" and ");
            sentenceBuilder.Append(noun3);
            sentenceBuilder.Append(".");

            return sentenceBuilder.ToString();
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
                if (index >= 0)
                {
                    // Find the verb and the direct object
                    string verbPhrase = null;
                    string directObject = null;
                    for (int i = index + 1; i < words.Length; i++)
                    {
                        string word = words[i];
                        if (Grammar.IsVerb(word))
                        {
                            verbPhrase = word;
                            break;
                        }
                    }
                    if (verbPhrase != null && index < words.Length - 1)
                    {
                        directObject = string.Join(" ", words, index + 2, words.Length - index - 2);
                        if (directObject.StartsWith("a ") || directObject.StartsWith("an "))
                        {
                            directObject = "the " + directObject;
                        }
                    }
                    if (verbPhrase != null && directObject != null)
                    {
                        return $"{subject} {verbPhrase} {directObject}";
                    }
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
                    // Extract all predicates after the subject and verb, separated by commas
                    var predicates = new List<string>();
                    for (int i = index + 1; i < words.Length; i++)
                    {
                        if (words[i] == ",") continue;
                        if (i < words.Length - 1 && words[i] == "and" && words[i + 1] == verb) continue;
                        predicates.Add(words[i]);
                    }
                    return string.Join(" ", predicates);
                }
            }
            return null;
        }

        private static string ExtractDefaultAnswer(string subject, string verb, string[] words)
        {
            // check if subject is a pronoun
            bool isPronoun = Grammar.IsPronoun(subject);

            // create a regular expression pattern to match the verb
            string verbPattern = verb;
            if (!string.IsNullOrEmpty(verb))
            {
                if (verb.EndsWith("s"))
                {
                    verbPattern = $"{verb.Substring(0, verb.Length - 1)}[s]";
                }
                else if (verb.EndsWith("ed") || verb.EndsWith("d"))
                {
                    verbPattern = $"{verb.Substring(0, verb.Length - 2)}[e]?[d]";
                }
            }

            // create a regular expression pattern to match the predicate
            string predicatePattern = $"({verbPattern})\\s+(.*)";
            if (isPronoun)
            {
                predicatePattern = $"({verbPattern})\\s+(a|an|the\\s+)?(.*)";
            }

            // loop through the words to find the predicate
            string predicate = null;
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];

                // check if the word matches the predicate pattern
                Match match = Regex.Match(word, predicatePattern);
                if (match.Success)
                {
                    // get the predicate from the match and return it
                    predicate = match.Groups[2].Value;
                    if (isPronoun)
                    {
                        predicate = $"{match.Groups[3].Value}";
                        subject = $"{match.Groups[2].Value.Trim()} {subject}";
                    }
                    break;
                }
            }

            // replace any pronouns in the predicate with the subject
            if (predicate != null && isPronoun)
            {
                predicate = Grammar.ReplacePronoun(subject, predicate);
            }

            return predicate;
        }

        public static string GetRestOfSentence(string[] words, int startIndex, string delimiter = ".")
        {
            if (words == null || startIndex < 0 || startIndex >= words.Length)
            {
                return "";
            }

            string restOfSentence = string.Join(" ", words, startIndex, words.Length - startIndex);
            return restOfSentence + delimiter;
        }

    }
}
