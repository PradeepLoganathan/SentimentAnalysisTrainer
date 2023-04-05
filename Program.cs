using System;
using Microsoft.ML;
using System.Threading.Tasks;
using DataStructures;
using ModelTraining;

namespace SentimentAnalysisConsoleApp
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
           
            ModelBuilder modelBuilder = new ModelBuilder();
            await modelBuilder.LoadTrainingData();
            modelBuilder.TrainTestSplit();
            modelBuilder.PrepareData();
            modelBuilder.Train();
            modelBuilder.PrintModelMetrics();
            modelBuilder.CreateModelMetrics();
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
      
    }
}