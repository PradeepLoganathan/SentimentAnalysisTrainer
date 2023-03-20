using System;
using System.IO;
using Microsoft.ML;
using static Microsoft.ML.DataOperationsCatalog;
using Microsoft.ML.Trainers;
using System.Net.Http;
using System.Threading.Tasks;

using Common;
using DataStructures;
using ModelRepository;
using ModelTraining;

namespace SentimentAnalysisConsoleApp
{
    internal static class Program
    {

        static async Task Main(string[] args)
        {
            SettingsReader settingsReader = new SettingsReader();
            var repoToken = settingsReader.ReadSection<RepoToken>("GithubToken");

            var trainingDataRepo = new Github(repoToken.Token);
            trainingDataRepo.SetRepo("PradeepLoganathan", "SentimentAnalysisTrainer");
            var modelRepo = new Github(repoToken.Token);
            modelRepo.SetRepo("PradeepLoganathan", "ModelRepository");

            ModelBuilder modelBuilder = new ModelBuilder(trainingDataRepo, modelRepo);
            await modelBuilder.SaveModel();
            
            await modelBuilder.LoadTrainingData();
            modelBuilder.TrainTestSplit();
            modelBuilder.PrepareData();
            modelBuilder.Train();
            modelBuilder.PrintModelMetrics();
            modelBuilder.SaveModelMetrics();
            await modelBuilder.SaveModel();
        }

       
        private static void TestPrediction(MLContext mlContext, ITransformer trainedModel)
        {
            // TRY IT: Make a single test prediction, loading the model from .ZIP file
            SentimentIssue sampleStatement = new SentimentIssue { Text = "He is an amazing person!" };

            // Create prediction engine related to the loaded trained model
            var predEngine = mlContext.Model.CreatePredictionEngine<SentimentIssue, SentimentPrediction>(trainedModel);

            // Score
            var resultprediction = predEngine.Predict(sampleStatement);

            Console.WriteLine($"=============== Single Prediction  ===============");
            Console.WriteLine($"Text: {sampleStatement.Text} | Prediction: {(Convert.ToBoolean(resultprediction.Prediction) ? "Toxic" : "Non Toxic")} sentiment | Probability of being toxic: {resultprediction.Probability} ");
            Console.WriteLine($"================End of Process.Hit any key to exit==================================");
            Console.ReadLine();
        }

        public static string GetAbsolutePath(string relativePath)
        {
            FileInfo _dataRoot = new FileInfo(typeof(Program).Assembly.Location);
            string assemblyFolderPath = _dataRoot.Directory.FullName;

            string fullPath = Path.Combine(assemblyFolderPath, relativePath);

            return fullPath;
        }
    }
}