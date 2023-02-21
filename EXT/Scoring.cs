using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace SelfTrainingBot.NLP
{
    public class Scoring
    {
        public double GetDoubleScore(string string1, string string2)
        {
            Console.WriteLine($"{string1} = {string2}");

            while (true)
            {
                Console.Write("Enter a double value: ");
                string input = Console.ReadLine();

                if (double.TryParse(input, out double result))
                {
                    return result;
                }

                Console.WriteLine("Invalid input. Please enter a valid double value.");
            }
        }
    }
}