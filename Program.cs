using System;
using System.IO;
using Microsoft.ML;
using SentimentAnalysisConsoleApp.DataStructures;
using Common;
using static Microsoft.ML.DataOperationsCatalog;
using Microsoft.ML.Trainers;
using System.Net.Http;

namespace SentimentAnalysisConsoleApp
{
    internal static class Program
    {
        private static readonly string BaseDatasetsRelativePath = @"Data";
        private static readonly string DataRelativePath = $"{BaseDatasetsRelativePath}/wikiDetoxAnnotated40kRows.tsv";

        private static readonly string DataPath = GetAbsolutePath(DataRelativePath);

        private static readonly string BaseModelsRelativePath = @"../../../MLModels";
        private static readonly string ModelRelativePath = $"{BaseModelsRelativePath}/SentimentModel.zip";

        private static readonly string ModelPath = GetAbsolutePath(ModelRelativePath);

        private static string wikiDetoxPath = "wikiDetoxAnnotated40kRows.tsv";

        static async void Main(string[] args)
        {
            // Create MLContext 
            // Set a random seed for repeatable/deterministic results across multiple trainings.
            var mlContext = new MLContext(seed: 1);

            if (!File.Exists(wikiDetoxPath))
            {
                var contents = await new HttpClient()
                    .GetStringAsync("https://raw.githubusercontent.com/PradeepLoganathan/SentimentAnalysisTrainer/main/SentimentAnalysisTrainer/Data/wikiDetoxAnnotated40kRows.tsv");

                File.WriteAllText(wikiDetoxPath, contents);
            }

            // Load data
            IDataView dataView = mlContext.Data.LoadFromTextFile<SentimentIssue>(wikiDetoxPath, hasHeader: true);

            TrainTestData trainTestSplit = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
            IDataView trainingData = trainTestSplit.TrainSet;
            IDataView testData = trainTestSplit.TestSet;

            // Data preparation         
            var dataProcessPipeline = mlContext.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(SentimentIssue.Text));

            // Select algorithm and configure model builder                            
            var trainer = mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features");
            var trainingPipeline = dataProcessPipeline.Append(trainer);

            // Train the model
            ITransformer trainedModel = trainingPipeline.Fit(trainingData);
            EvaluateModel(mlContext, testData, trainer, trainedModel);

            // Persist the trained model to a .ZIP file
            mlContext.Model.Save(trainedModel, trainingData.Schema, ModelPath);

            Console.WriteLine("The model is saved to {0}", ModelPath);

            TestPrediction(mlContext, trainedModel);
        }

        private static void EvaluateModel(MLContext mlContext, IDataView testData, SdcaLogisticRegressionBinaryTrainer trainer, ITransformer trainedModel)
        {
            // Evaluate the model
            var predictions = trainedModel.Transform(testData);
            var metrics = mlContext.BinaryClassification.Evaluate(data: predictions, labelColumnName: "Label", scoreColumnName: "Score");

            // Display model accuracy stats
            ConsoleHelper.PrintBinaryClassificationMetrics(trainer.ToString(), metrics);
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