using HtmlAgilityPack;
using System.Text;

namespace SelfTrainingBot.HTML
{
    internal class Articles
    {

        private static readonly HashSet<string> StopWords = new HashSet<string>
    {
        "the", "and", "a", "to", "that", "as", "on", "be", "by", "at",
        "an", "this", "or", "but", "not", "is"
    };

        private static string preProcess(string input)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in input)
            {
                if (Char.IsLetter(c) || Char.IsWhiteSpace(c))
                {
                    sb.Append(c);
                }
            }
            string[] words = sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words);
        }

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
            if (!Uri.IsWellFormedUriString(articleLink, UriKind.Absolute))
            {
                throw new ArgumentException("Invalid URL provided");
            }

            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(articleLink);
                    var content = await response.Content.ReadAsStringAsync();

                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(content);
                    var articleNodes = htmlDocument.DocumentNode.DescendantsAndSelf().Where(n => n.Name == "p");

                    var articleText = new StringBuilder();
                    foreach (var node in articleNodes)
                    {
                        articleText.AppendLine(node.InnerText);
                    }

                    return preProcess(articleText.ToString());
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Error retrieving article content: " + ex.Message);
            }
            catch (HtmlWebException ex)
            {
                throw new Exception("Error parsing article HTML: " + ex.Message);
            }
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

            // Join the keywords in the array using the delimiter
            var delimiter = ", ";
            var keywordString = string.Join(delimiter, keywords);

            // Output each keyword as a separate item in the console
            Console.WriteLine("Keywords:");
            foreach (var keyword in keywords)
            {
                Console.WriteLine(keyword);
            }

            return keywords;
        }


    }
}
