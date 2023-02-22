using System.Text;

namespace SelfTrainingBot.EXT
{
    public class Grammar
    {

        static HashSet<string> verbs = new HashSet<string>() { "be", "have", "do", "say", "get", "make", "go", "know", "take", "see", "come", "think", "look", "want", "give", "use", "find", "tell", "ask", "work", "seem", "feel", "try", "leave", "call", "run", "walk", "jump", "eat", "sleep", "talk", "write", "read", "sing", "dance", "drink", "smile", "laugh", "cry", "play", "swim", "drive", "fly", "buy", "sell", "pay", "meet", "visit", "return", "wait", "stand", "sit", "lie", "think", "believe", "remember", "forget", "understand", "hear", "see", "listen", "smell", "taste", "touch", "cut", "break", "cook", "wash", "clean", "paint", "draw", "watch", "study", "learn", "teach", "explain", "show", "change", "improve", "fix", "repair", "open", "close", "lock", "unlock", "turn", "push", "pull", "lift", "drop", "carry", "throw", "catch", "release", "shoot", "kill", "die", "live", "survive", "win", "lose", "succeed", "fail", "help", "hurt", "love", "hate", "like", "dislike", "need", "want", "hope", "wish", "dream", "imagine", "create", "destroy", "develop", "produce", "manufacture", "sell", "buy", "own", "rent", "borrow", "lend", "owe", "pay", "save", "invest", "spend", "earn", "lose", "steal", "rob", "cheat", "lie", "betray", "forgive", "apologize", "thank", "welcome", "congratulate", "celebrate", "condemn", "protest", "support", "oppose", "vote", "elect", "appoint", "resign", "retire", "promote", "demote", "hire", "fire", "quit", "dismiss", "sack", "sue", "arrest", "convict", "sentence", "punish", "pardoned", "release", "escape", "extradite", "emigrate", "immigrate", "travel", "visit", "return", "move", "settle", "adjust", "adapt", "accommodate", "resist", "struggle", "fight", "attack", "defend", "invade", "occupy", "liberate", "bomb", "destroy", "rebuild", "reconstruct", "negotiate", "mediat", "arbitrate", "compromise", "cooperate", "compete", "play", "watch", "follow", "obey", "break", "violate", "defy", "challenge", "provoke", "insult", "flatter", "compliment", "praise", "criticize", "blame", "accuse", "apologize", "forgive", "justify" };


        public static string GetVerb(string[] words)
        {
            string verb = null;

            Parallel.For(0, words.Length, (i, state) =>
            {
                if (verbs.Contains(words[i]))
                {
                    verb = words[i];
                    state.Stop();
                }
            });

            return verb;
        }
        public static string[] GetVerbs(string sentence)
        {
            string[] words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string[] foundVerbs = null;

            Parallel.For(0, words.Length, (i, state) =>
            {
                if (verbs.Contains(words[i]))
                {
                    lock (foundVerbs)
                    {
                        if (foundVerbs == null)
                        {
                            foundVerbs = new string[] { words[i] };
                        }
                        else
                        {
                            string[] newVerbs = new string[foundVerbs.Length + 1];
                            Array.Copy(foundVerbs, newVerbs, foundVerbs.Length);
                            newVerbs[newVerbs.Length - 1] = words[i];
                            foundVerbs = newVerbs;
                        }
                    }

                    state.Stop();
                }
            });

            return foundVerbs;
        }

        public static string GetSubject(string[] words)
        {
            string subject = null;
            object lockObject = new object();

            Parallel.ForEach(words, (word, state) =>
            {
                if (Grammar.IsNoun(word))
                {
                    lock (lockObject)
                    {
                        if (subject == null)
                        {
                            subject = word;
                            state.Break();
                        }
                    }
                }
            });

            return subject;
        }
        public static int GetSubjectIndex(string[] words)
        {
            for (int i = 0; i < words.Length; i++)
            {
                if (IsNoun(words[i]))
                {
                    return i;
                }
            }

            return -1;
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

        public static bool IsPronoun(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;

            // Split the string into words
            string[] words = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Use parallelism to check for pronouns
            bool isPronoun = Parallel.ForEach(words, (word, state) =>
            {
                switch (word.ToLower())
                {
                    case "i":
                    case "you":
                    case "he":
                    case "she":
                    case "it":
                    case "we":
                    case "they":
                        state.Stop();
                        break;
                }
            }).IsCompleted;

            return isPronoun;
        }

        public static string ReplacePronoun(string pronoun, string noun)
        {
            var result = new StringBuilder();
            Parallel.ForEach(pronoun.Split(), word =>
            {
                switch (word)
                {
                    case "I":
                        result.Append("You are ");
                        break;
                    case "you":
                        result.Append("I am ");
                        break;
                    case "he":
                        result.Append("He is ");
                        break;
                    case "she":
                        result.Append("She is ");
                        break;
                    case "it":
                        result.Append("It is ");
                        break;
                    case "we":
                        result.Append("We are ");
                        break;
                    case "they":
                        result.Append("They are ");
                        break;
                    default:
                        result.Append(word + " is ");
                        break;
                }
            });
            result.Append(noun);
            return result.ToString();
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

            // Find the verb in the sentence
            string verb = GetVerb(words);
            if (verb == null)
            {
                result[0] = "";
                result[1] = "";
                return result;
            }

            result[0] = verb;

            // Check the tense of the verb
            if (verb.EndsWith("ing", StringComparison.OrdinalIgnoreCase))
            {
                result[1] = "present continuous";
            }
            else if (verb.EndsWith("ed", StringComparison.OrdinalIgnoreCase))
            {
                result[1] = "past";
            }
            else if (verb == "will" || verb == "shall")
            {
                result[1] = "future simple";
            }
            else if (verb == "going")
            {
                // Look for the next verb to determine the tense
                string nextVerb = FindNextVerb(words, verb);
                if (nextVerb == null)
                {
                    result[1] = "going to future";
                }
                else if (nextVerb.EndsWith("ing", StringComparison.OrdinalIgnoreCase))
                {
                    result[1] = "going to present continuous";
                }
                else if (nextVerb.EndsWith("ed", StringComparison.OrdinalIgnoreCase))
                {
                    result[1] = "going to past";
                }
                else
                {
                    result[1] = "going to future";
                }
            }
            else if (verb.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                result[1] = "present simple, third person singular";
            }
            else
            {
                result[1] = "infinitive";
            }

            return result;
        }
        public static string FindNextVerb(string[] words, string currentVerb)
        {
            // Loop over the words after the current verb to find the next verb
            for (int i = Array.IndexOf(words, currentVerb) + 1; i < words.Length; i++)
            {
                string word = words[i];
                if (verbs.Contains(word))
                {
                    return word;
                }
                else if (word == "to")
                {
                    // Skip over "to"
                    continue;
                }
                else
                {
                    // Stop searching if a non-verb is found
                    break;
                }
            }

            return null;
        }

        public static bool IsNoun(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return false;
            }

            // Check if the word is capitalized (assuming that capitalized words are proper nouns)
            if (char.IsUpper(word[0]))
            {
                return true;
            }

            // Check if the word is a plural noun (assuming that plural nouns end in "s" or "es")
            if (word.Length > 2 && word.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                string singular = word.Substring(0, word.Length - 1);

                // Check if the singular form of the word is a valid noun (assuming that singular nouns are capitalized)
                return char.IsUpper(singular[0]);
            }

            return false;
        }

        public static bool IsConjunction(string word)
        {
            string[] conjunctions = { "and", "but", "or", "yet", "so", "for", "nor" };
            return conjunctions.Contains(word.ToLower());
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
