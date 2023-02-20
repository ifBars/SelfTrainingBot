using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTrainingBot
{
    public class Translate
    {
        private static readonly string key = "a428a0a3f1214421a6212d728d39c6f4";
        private static readonly string endpoint = "https://api.cognitive.microsofttranslator.com";

        private static readonly string location = "westcentralus";

        private static string preProcess(string sourceLanguage)
        {
            if (sourceLanguage.Equals("english", StringComparison.OrdinalIgnoreCase))
            {
                sourceLanguage = "en";
            }

            if (sourceLanguage.Equals("spanish", StringComparison.OrdinalIgnoreCase))
            {
                sourceLanguage = "es";
            }

            if (sourceLanguage.Equals("bulgarian", StringComparison.OrdinalIgnoreCase))
            {
                sourceLanguage = "bg";
            }

            if (sourceLanguage.Equals("chinese", StringComparison.OrdinalIgnoreCase))
            {
                sourceLanguage = "zh";
            }

            if (sourceLanguage.Equals("danish", StringComparison.OrdinalIgnoreCase))
            {
                sourceLanguage = "da";
            }

            if (sourceLanguage.Equals("czech", StringComparison.OrdinalIgnoreCase))
            {
                sourceLanguage = "cs";
            }

            if (sourceLanguage.Equals("german", StringComparison.OrdinalIgnoreCase))
            {
                sourceLanguage = "de";
            }

            if (sourceLanguage.Equals("greek", StringComparison.OrdinalIgnoreCase))
            {
                sourceLanguage = "el";
            }

            if (sourceLanguage.Equals("finnish", StringComparison.OrdinalIgnoreCase))
            {
                sourceLanguage = "fi";
            }

            if (sourceLanguage.Equals("french", StringComparison.OrdinalIgnoreCase))
            {
                sourceLanguage = "fr";
            }

            if (sourceLanguage.Equals("italian", StringComparison.OrdinalIgnoreCase))
            {
                sourceLanguage = "it";
            }

            if (sourceLanguage.Equals("japanese", StringComparison.OrdinalIgnoreCase))
            {
                sourceLanguage = "ja";
            }

            if (sourceLanguage.Equals("korean", StringComparison.OrdinalIgnoreCase))
            {
                sourceLanguage = "ko";
            }

            if (sourceLanguage.Equals("russian", StringComparison.OrdinalIgnoreCase))
            {
                sourceLanguage = "ru";
            }

            if (sourceLanguage.Equals("ukrainian", StringComparison.OrdinalIgnoreCase))
            {
                sourceLanguage = "uk";
            }

            return sourceLanguage;
        }

        public static async Task<string> TranslateText(string textToTranslate, string sourceLanguage, string targetLanguage)
        {

            sourceLanguage = preProcess(sourceLanguage);
            targetLanguage = preProcess(targetLanguage);

            // Input and output languages are defined as parameters.
            string route = $"/translate?api-version=3.0&from={sourceLanguage}&to={targetLanguage}";
            object[] body = new object[] { new { Text = textToTranslate } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                // Build the request.
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(endpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                // location required if you're using a multi-service or regional (not global) resource.
                request.Headers.Add("Ocp-Apim-Subscription-Region", location);

                // Send the request and get response.
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                // Read response as a string.
                string result = await response.Content.ReadAsStringAsync();

                // Use Newtonsoft.Json to deserialize the JSON string
                dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(result);

                // Get the text from the first translation
                string text = json[0]["translations"][0]["text"];

                // Output the text to the console
                Console.WriteLine(text);

                return result;
            }
        }

    }
}
