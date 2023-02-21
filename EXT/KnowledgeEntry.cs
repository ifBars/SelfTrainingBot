using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTrainingBot.NLP
{
    public class KnowledgeEntry
    {
        public string Subject { get; set; }
        public string Verb { get; set; }
        public string Tense { get; set; }
        public List<string> GeneratedQuestions { get; set; }
        public List<string> GeneratedAnswers { get; set; }
        public List<double> Scores { get; set; }

        public KnowledgeEntry()
        {
            GeneratedQuestions = new List<string>();
            GeneratedAnswers = new List<string>();
            Scores = new List<double>();
        }
    }
}
