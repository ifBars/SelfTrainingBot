using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SelfTrainingBot.HTML
{
    internal class Articles
    {

        private static readonly HashSet<string> StopWords = new HashSet<string>
    {
        "the", "and", "a", "to", "in", "that", "it", "as", "for", "on", "are", "be", "by", "at",
        "an", "this", "or", "but", "not", "is"
    };

        public static string RemoveStopWords(string input)
        {
            // Split the input string into words
            var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Remove stop words from the array of words
            words = words.Where(w => !StopWords.Contains(w.ToLower())).ToArray();

            // Combine the remaining words into a new string
            var output = string.Join(" ", words);

            return output;
        }

        public static async Task<string> ExtractArticle(string articleLink)
        {
            // Retrieve the article text from the HTTP link
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(articleLink);
            var content = await response.Content.ReadAsStringAsync();

            // Parse the HTML to extract the article text
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(content);
            var articleText = htmlDocument.DocumentNode.InnerText;

            return articleText;
        }

        public static async Task<string[]> ExtractKeywordsFromArticle(string articleLink)
        {
            // Retrieve the article text from the HTTP link
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(articleLink);
            var content = await response.Content.ReadAsStringAsync();
            if (content == null)
            {
                Console.WriteLine("Debug: Article content is null");
            }

            // Parse the HTML to extract the article text
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(content);
            var articleText = htmlDocument.DocumentNode.InnerText;
            if (articleText == null)
            {
                Console.WriteLine("Debug: Article text is null");
            }

            // Preprocess the article text
            articleText = articleText.ToLowerInvariant();
            articleText = RemoveStopWords(articleText); // implement your own RemoveStopWords method

            // Tokenize the article text
            var separators = new[] { ' ', '\n', '\r', '\t' };
            var keywords = articleText.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            return keywords;
        }


    }
}
