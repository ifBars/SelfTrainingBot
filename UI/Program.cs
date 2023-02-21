using Newtonsoft.Json;
using SelfTrainingBot.FT;
using SelfTrainingBot.HTML;
using SelfTrainingBot.NLP;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var bot = new ChatBot();
        bool doGen = true;

        async Task AwaitKeywords(string input)
        {
            try
            {
                Console.WriteLine("Debug: Awaiting keywords");
                string modifiedInput = input.Replace("train ", "");

                string[] keywords = await Articles.ExtractKeywordsFromArticle(modifiedInput);

                Console.WriteLine("Debug: Extracting article sentences");
                string article = await Articles.ExtractArticle(modifiedInput);
                string[] sentences = article.Split(new char[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                List<Tuple<string, string>> qaPairs = new List<Tuple<string, string>>();
                Random ran = new Random();

                foreach (string sentence in sentences)
                {
                    string[] words = sentence.Split(' ');
                    int sentenceLength = ran.Next(6, 21);
                    int startIndex = 0;

                    while (startIndex + sentenceLength < words.Length)
                    {
                        // get the next sentence of random length
                        string currentSentence = string.Join(" ", words.Skip(startIndex).Take(sentenceLength));
                        string nextWord = words[startIndex + sentenceLength];
                        string nextSentence = currentSentence + " " + nextWord;

                        if (nextWord == nextWord.ToUpper() || nextWord == nextWord.ToLower() || nextWord.EndsWith("."))
                        {
                            // next word is capitalized or ends with a period, keep the sentences separate
                            startIndex += sentenceLength;
                        }
                        else
                        {
                            // combine the current sentence and the next word into one sentence
                            currentSentence = nextSentence;
                            sentenceLength++;
                            startIndex += sentenceLength;
                        }

                        // check if the sentence contains any of the keywords
                        if (keywords.Any(keyword => currentSentence.Contains(keyword)))
                        {
                            Console.WriteLine("Debug: Generating Question");
                            // convert the sentence into a question
                            string question = QnA.ConvertToQuestion(currentSentence);

                            Console.WriteLine("Debug: Generating Answer");
                            // extract the answer from the sentence
                            string answer = QnA.ExtractAnswer(currentSentence, question);

                            // add the question and answer to the list of QA pairs
                            qaPairs.Add(new Tuple<string, string>(question, answer));

                            Scoring scorer = new Scoring();
                            double pairScore = scorer.GetDoubleScore(question, answer);

                            foreach(string genwords in words)
                            {
                                Console.WriteLine("Debug: Word Entry - " + genwords);
                            }

                            string genwords2 = string.Join(" ", words.Take(startIndex + sentenceLength));
                            string keywordString = string.Join(" ", keywords);
                            foreach (string keyword in keywords)
                            {
                                genwords2 = genwords2.Replace(keyword, keyword + " ");
                            }
                            Console.WriteLine("Debug: Words - " + genwords2);

                            Console.WriteLine("Debug: Training dictionary commencing");
                            Console.WriteLine("Debug: Score - " + pairScore.ToString());
                            Console.WriteLine("Debug: Question - " + question);
                            Console.WriteLine("Debug: Answer - " + answer);
                            QnA.Train(pairScore, genwords2, question, answer);

                        }
                    }

                    // add any remaining words as the last sentence
                    if (startIndex < words.Length)
                    {
                        string remainingSentence = string.Join(" ", words.Skip(startIndex));
                        if (keywords.Any(keyword => remainingSentence.Contains(keyword)))
                        {
                            string question = QnA.ConvertToQuestion(remainingSentence);
                            string answer = QnA.ExtractAnswer(remainingSentence, question);
                            qaPairs.Add(new Tuple<string, string>(question, answer));

                            Scoring scorer = new Scoring();
                            double pairScore = scorer.GetDoubleScore(question, answer);

                            foreach(string genwords in words)
                            {
                                Console.WriteLine("Debug: Word Entry - " + genwords);
                            }

                            string genwords2 = string.Join(" ", words.Take(startIndex + sentenceLength));
                            string keywordString = string.Join(" ", keywords);
                            foreach (string keyword in keywords)
                            {
                                genwords2 = genwords2.Replace(keyword, keyword + " ");
                            }
                            Console.WriteLine("Debug: Words - " + genwords2);

                            Console.WriteLine("Debug: Training dictionary commencing");
                            Console.WriteLine("Debug: Score - " + pairScore.ToString());
                            Console.WriteLine("Debug: Question - " + question);
                            Console.WriteLine("Debug: Answer - " + answer);
                            QnA.Train(pairScore, genwords2, question, answer);

                        }
                    }
                }

                double score = double.MinValue;
                List<double> scores = new List<double>();
                List<Tuple<string, string>> pair2 = new List<Tuple<string, string>>();

                foreach (Tuple<string, string> qaPair in qaPairs)
                {
                    if (qaPair.Item1 != null && qaPair.Item1 != "I don't know.")
                    {
                        if (qaPair.Item2 != null && qaPair.Item2 != "I don't know.")
                        {
                            pair2.Add(qaPair);
                            Scoring scorer = new Scoring();
                            double pairScore = scorer.GetDoubleScore(qaPair.Item1, qaPair.Item2);
                            scores.Add(pairScore);
                            score = Math.Max(score, pairScore);
                        }
                    }
                }

                Console.WriteLine("Debug: Printing results");
                // write the QA pairs to a JSON file
                var result = new { pairs = pair2, scores = scores };
                using (StreamWriter file = File.CreateText("output.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, result);
                }

                if (qaPairs.Count == 0)
                {
                    Console.WriteLine("Debug: No QA pairs found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        while (true)
        {
            doGen = true;
            Console.Write("> ");
            string input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                break;
            }

            if (string.Equals(input, "clear", StringComparison.OrdinalIgnoreCase) == true)
            {
                Console.Clear();
                doGen = false;
            }

            if (input.StartsWith("wiki ", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Debug: Wiki search commencing");
                NLP.Wiki(input);

                doGen = false;
            }

            if (input.StartsWith("translate ", StringComparison.OrdinalIgnoreCase))
            {
                string modifiedInput = input.Replace("translate ", "");

                Console.WriteLine("Debug: Translation commencing");
                string sourceLang = string.Empty;
                string targetLang = string.Empty;

                string[] words = modifiedInput.Split(' ');

                // Look for the words "from" and "to".
                for (int i = 0; i < words.Length - 1; i++)
                {
                    if (words[i].Equals("from", StringComparison.OrdinalIgnoreCase))
                    {
                        sourceLang = words[i + 1];
                        words[i + 1] = string.Empty;
                    }
                    else if (words[i].Equals("to", StringComparison.OrdinalIgnoreCase))
                    {
                        targetLang = words[i + 1];
                        words[i + 1] = string.Empty;
                    }
                }

                modifiedInput = modifiedInput.Replace("from", "");
                modifiedInput = modifiedInput.Replace("to", "");

                string transl = await Translate.TranslateText(modifiedInput, sourceLang, targetLang);

                Console.WriteLine("Here is your translated text");

                doGen = false;
            }

            if (input.StartsWith("train ", StringComparison.OrdinalIgnoreCase) && input.Contains("http", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Debug: Training commencing");
                await AwaitKeywords(input);

                doGen = false;
            }

            if (StringMatchingTools.SMT.Check(input, "What day is it today?", false) > 0.7)
            {
                Console.WriteLine("The current date today is: " + DateTime.Now.Date.ToString());
                doGen = false;
            }

            if (StringMatchingTools.SMT.Check(input, "What time is it?", false) > 0.7)
            {
                Console.WriteLine("The current time is: " + DateTime.Now.TimeOfDay.ToString());
                doGen = false;
            }

            if (doGen == true)
            {
                string response = bot.GenerateResponse(input);
                if (response == null)
                {
                    Console.WriteLine("I'm sorry, I don't understand. What should I say?");
                    string newResponse = Console.ReadLine();
                    bot.Train(input, newResponse);
                    response = newResponse;

                }
                Console.WriteLine(response);
            }


        }
    }

}