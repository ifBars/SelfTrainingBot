using SelfTrainingBot;
using System;

internal class Program
{
    private static void Main(string[] args)
    {
        var bot = new ChatBot();
        bool doGen = true;

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

            if (input.StartsWith("train ", StringComparison.OrdinalIgnoreCase) && input.Contains("http", StringComparison.OrdinalIgnoreCase))
            {
                string modifiedInput = input.Remove("train ");
                string[] keywords = Articles.ExtractKeywordsFromArticle(input);
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
                    Console.WriteLine("I'm sorry, I don't understand.");
                    Console.Write("What should I say? ");
                    string newResponse = Console.ReadLine();
                    bot.Train(input, newResponse);
                    response = newResponse;
                    
                }
                Console.WriteLine(response);
            }

            
        }
    }

}