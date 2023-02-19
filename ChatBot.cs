using System;
using System.IO;
using System.Text.Json;
using System.Xml;

using StringMatchingTools;
using System.Collections.Generic;
using Microsoft.VisualBasic;

public class ChatBot
{
    private string _trainingDataFile = "training.json";
    private Dictionary<string, string> _trainingData;

    public ChatBot()
    {
        LoadTrainingData();
    }

    private void LoadTrainingData()
    {
        if (!File.Exists(_trainingDataFile))
        {
            _trainingData = new Dictionary<string, string>();
            SaveTrainingData(_trainingDataFile);
        }
        else
        {
            _trainingData = LoadTrainingData(_trainingDataFile);
        }
    }

    private void SaveTrainingData(string filePath)
    {
        var jsonString = System.Text.Json.JsonSerializer.Serialize(_trainingData);
        File.WriteAllText(filePath, jsonString);
    }

    public string GenerateResponse(string input)
    {
        string closestMatch = null;
        List<string> matches = new List<string>();

        foreach (string question in _trainingData.Keys)
        {
            double distance = SMT.Check(input, question, false);
            Console.WriteLine("Debug: " + distance.ToString());
            if (distance > 0.7)
            {
                matches.Add(question);
            }
        }

        if (matches.Count != 0)
        {
            closestMatch = matches[0];
        }

        if (closestMatch != null)
        {
            return _trainingData[closestMatch];
        }

        return null;
    }

    private Dictionary<string, string> LoadTrainingData(string filePath)
    {
        var jsonString = File.ReadAllText(filePath);
        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
        return data;
    }

    public void Train(string question, string response)
    {
        if (_trainingData.ContainsKey(question))
        {
            Console.WriteLine($"I already know the answer to \"{question}\". I will update it to \"{response}\".");
            _trainingData[question] = response;
        }
        else
        {
            _trainingData.Add(question, response);
            Console.WriteLine($"Got it. I will remember that the answer to \"{question}\" is \"{response}\".");
        }

        SaveTrainingData(_trainingDataFile);
    }

}