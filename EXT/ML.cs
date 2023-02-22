using Microsoft.ML;
using Microsoft.ML.Data;
using SelfTrainingBot.NLP;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SelfTrainingBot.EXT
{
    public class ML
    {
        private readonly MLContext mlContext = new MLContext();
        private ITransformer model;

        public string[] AnalyzeData(string[] inputData)
        {
            // Load the data into a DataView object
            var dataView = mlContext.Data.LoadFromEnumerable(LoadData(inputData).AsEnumerable());

            // Generate better questions using the model
            var predictions = model.Transform(dataView);
            var clusterIds = predictions.GetColumn<uint>("PredictedClusterId").ToArray();
            var questions = new List<string>();
            for (int i = 0; i < clusterIds.Length; i++)
            {
                if (clusterIds[i] == 0) // Cluster 0 corresponds to the most common questions
                {
                    questions.Add(inputData[i]);
                }
            }

            // Return the generated questions
            return questions.ToArray();
        }

        public void TrainModel()
        {
            // Load the data into an IDataView object
            var dataView = mlContext.Data.LoadFromEnumerable(LoadData().AsEnumerable());

            // Define the ML pipeline
            var pipeline = mlContext.Transforms.Text.FeaturizeText("Features", "Text")
                .Append(mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(mlContext.Transforms.ProjectToPrincipalComponents("FeaturesPCA", "Features", rank: 5))
                .Append(mlContext.Clustering.Trainers.KMeans("FeaturesPCA", numberOfClusters: 10));

            // Fit the pipeline to the data
            model = pipeline.Fit(dataView);
        }

        private DataTable LoadData()
        {
            // Convert the input data to a DataTable object
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("Subject", typeof(string));
            dataTable.Columns.Add("Verb", typeof(string));
            dataTable.Columns.Add("Tense", typeof(string));
            dataTable.Columns.Add("Text", typeof(string));
            dataTable.Columns.Add("Label", typeof(string));

            // Add rows to the DataTable
            foreach (var entry in KnowledgeEntry.knowledgeBase.Values)
            {
                foreach (var question in entry.GeneratedQuestions)
                {
                    DataRow row = dataTable.NewRow();
                    row["Subject"] = entry.Subject;
                    row["Verb"] = entry.Verb;
                    row["Tense"] = entry.Tense;
                    row["Text"] = question;
                    row["Label"] = $"{entry.Subject} {entry.Verb} {entry.Tense}";
                    dataTable.Rows.Add(row);
                }
            }

            return dataTable;
        }

        private DataTable LoadData(string[] inputData, string[] outputData = null)
        {
            // Convert the input data to a DataTable object
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("Subject", typeof(string));
            dataTable.Columns.Add("Verb", typeof(string));
            dataTable.Columns.Add("Tense", typeof(string));
            dataTable.Columns.Add("GeneratedQuestions", typeof(List<string>));
            dataTable.Columns.Add("GeneratedAnswers", typeof(List<string>));
            dataTable.Columns.Add("Scores", typeof(List<double>));
            if (outputData != null)
            {
                dataTable.Columns.Add("Label", typeof(string));
            }

            // Add rows to the DataTable
            if (inputData.Length != KnowledgeEntry.knowledgeBase.Count)
            {
                throw new ArgumentException("inputData length does not match knowledgeBase length");
            }
            for (int i = 0; i < inputData.Length; i++)
            {
                var entry = KnowledgeEntry.knowledgeBase[inputData[i]];
                DataRow row = dataTable.NewRow();
                row["Subject"] = entry.Subject;
                row["Verb"] = entry.Verb;
                row["Tense"] = entry.Tense;
                row["GeneratedQuestions"] = entry.GeneratedQuestions;
                row["GeneratedAnswers"] = entry.GeneratedAnswers;
                row["Scores"] = entry.Scores;
                if (outputData != null)
                {
                    row["Label"] = outputData[i];
                }
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

    }
}
