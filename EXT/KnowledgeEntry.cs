namespace SelfTrainingBot.NLP
{
    public class KnowledgeEntry
    {
        public string Subject { get; set; }
        public string Verb { get; set; }
        public string Tense { get; set; }
        public List<string> GeneratedQuestions { get; set; }
        public List<string> GeneratedAnswers { get; set; }
        public List<string> Keywords { get; set; }
        public List<string> SimilarQuestions { get; set; }
        public List<double> Scores { get; set; }

        public KnowledgeEntry()
        {
            GeneratedQuestions = new List<string>();
            GeneratedAnswers = new List<string>();
            Keywords = new List<string>();
            SimilarQuestions = new List<string>();
            Scores = new List<double>();
        }

        public static Dictionary<string, KnowledgeEntry> knowledgeBase = new Dictionary<string, KnowledgeEntry>();
    }
}
