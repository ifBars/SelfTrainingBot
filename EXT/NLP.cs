using Newtonsoft.Json.Linq;
using System.Net;
using System.Text.Json;

class NLP
{
    public static async Task Wiki(string searchTerm)
    {
        string url = $"https://en.wikipedia.org/w/api.php?action=query&list=search&srsearch={searchTerm}&format=json";

        using (var httpClient = new HttpClient())
        {
            using (var response = await httpClient.GetAsync(url))
            {
                string jsonResult = await response.Content.ReadAsStringAsync();
                JObject jsonObject = JObject.Parse(jsonResult);
                JArray searchResults = (JArray)jsonObject["query"]["search"];

                foreach (JToken result in searchResults)
                {
                    string title = (string)result["title"];
                    Console.WriteLine(title);
                }

                string personDescription = await FindPersonDescriptionAsync(searchTerm);
                Console.WriteLine("Debug: Printing results");
                // write the QA pairs to a JSON file
                Console.WriteLine("Debug: Person Description: " + personDescription);
                Dictionary<string, string> keyValuePairs = ConvertPersonDescriptionToDictionary(personDescription);
                SaveTrainingData(keyValuePairs, "wikioutput.json");
            }
        }
    }


    public static Dictionary<string, string> ConvertPersonDescriptionToDictionary(string personDescription)
    {
        if (personDescription == null)
        {
            Console.WriteLine($"Debug: Person description is null.");
            return null;
        }

        var dictionary = new Dictionary<string, string>();

        // Split the person description into multiple entries using the new line character
        var entries = personDescription.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // Add each entry to the dictionary as a key-value pair
        foreach (var entry in entries)
        {
            // Split the entry into two parts using the first colon as a separator
            var separatorIndex = entry.IndexOf(':');
            if (separatorIndex >= 0)
            {
                var key = entry.Substring(0, separatorIndex).Trim();
                var value = entry.Substring(separatorIndex + 1).Trim();
                dictionary[key] = value;
            }
        }

        Console.WriteLine($"Debug: Converted person description to dictionary: {string.Join(", ", dictionary)}");
        return dictionary;
    }

    private static void SaveTrainingData(Dictionary<string, string> input, string filePath)
    {
        try
        {
            if (input == null || input.Count == 0)
            {
                throw new ArgumentException("Input cannot be null or empty.");
            }

            var jsonString = JsonSerializer.Serialize(input);
            File.WriteAllText(filePath, jsonString);
        }
        catch (Exception ex)
        {
            // Log the error and handle it appropriately
            Console.WriteLine($"Error saving training data to {filePath}: {ex.Message}");
        }
    }
    public static async Task<string> FindPersonDescriptionAsync(string personName)
    {
        string url = "https://en.wikipedia.org/w/api.php?action=query&prop=revisions&rvprop=content&format=json&titles=" + personName;

        using HttpClient client = new HttpClient();
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string jsonResult = await response.Content.ReadAsStringAsync();

        JObject jsonObject = JObject.Parse(jsonResult);
        JToken pages = jsonObject["query"]["pages"].First.First;

        string content = null;
        if (pages["revisions"] != null)
        {
            content = (string)pages["revisions"].First["*"];
        }

        Console.WriteLine("Debug: Returning person content: " + content);

        if (content == null)
        {
            Console.WriteLine($"Debug: Content for {personName} is null.");
            return "";
        }

        return content;
    }
}