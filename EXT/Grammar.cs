namespace SelfTrainingBot.EXT
{
    public class Grammar
    {

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

        public static string GetSubject(string[] words)
        {
            // look for the first noun in the sentence
            for (int i = 0; i < words.Length; i++)
            {
                if (Grammar.IsNoun(words[i]))
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

        public static string ReplacePronoun(string pronoun, string noun)
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
        public static bool IsPronoun(string word)
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
