using System.Threading.Tasks;
using System.Linq;
using System.IO;

using Common;
using DataStructures;
using Microsoft.ML;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms.Text;
using ModelRepository;
using static Microsoft.ML.DataOperationsCatalog;
using System;

namespace ModelTraining;

internal class ModelBuilder
{
    MLContext mlContext;
    string wikiDetoxRepoPath, wikiDetoxLocalFilePath, modelPath, metricPath;
    IDataView dataView, trainingData, testData;
    ITransformer trainedModel;
    Github trainingDataRepo, modelRepo;
    TextFeaturizingEstimator dataProcessPipeline;

    SdcaLogisticRegressionBinaryTrainer trainer;

    public ModelBuilder(Github TrainingDataRepo, Github ModelRepo):this()
    {
        trainingDataRepo = TrainingDataRepo;
        modelRepo = ModelRepo;
        mlContext = new MLContext(seed: 1);
    }

    public ModelBuilder()
    {
        wikiDetoxRepoPath = @"SentimentAnalysis/wikiDetoxAnnotated40kRows.tsv";
        wikiDetoxLocalFilePath = "wikiDetoxAnnotated40kRows.tsv";
        modelPath = "SentimentModel.zip";
        metricPath = "SentimentModel-Metrics.txt";

    }

    public async Task LoadTrainingData()
    {

        await trainingDataRepo.ReadFile(wikiDetoxRepoPath, wikiDetoxLocalFilePath);
        dataView = mlContext.Data.LoadFromTextFile<SentimentIssue>(wikiDetoxLocalFilePath, hasHeader: true);
    }

    public void TrainTestSplit()
    {
        TrainTestData trainTestSplit = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
        trainingData = trainTestSplit.TrainSet;
        testData = trainTestSplit.TestSet;
    }

    public void PrepareData()
    {
        // Data preparation         
        dataProcessPipeline = mlContext.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(SentimentIssue.Text));
    }

    public void Train()
    {
        Console.WriteLine($"Starting training...");
        // Select algorithm and configure model builder                            
        trainer = mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features");
        var trainingPipeline = dataProcessPipeline.Append(trainer);

        // Train the model
        trainedModel = trainingPipeline.Fit(trainingData);
        System.Console.WriteLine("Completed Training..");
        
    }

    public void PrintModelMetrics()
    {
        // Evaluate the model
        var predictions = trainedModel.Transform(testData);
        var metrics = mlContext.BinaryClassification.Evaluate(data: predictions, labelColumnName: "Label", scoreColumnName: "Score");

        // Display model accuracy stats
        ConsoleHelper.PrintBinaryClassificationMetrics(trainer.ToString(), metrics);
    }

    public void CreateModelMetrics()
    {
        System.Console.WriteLine("Printing model metrics...");
        // Evaluate the model
        var predictions = trainedModel.Transform(testData);
        var metrics = mlContext.BinaryClassification.Evaluate(data: predictions, labelColumnName: "Label", scoreColumnName: "Score");

        // Display model accuracy stats
        var metricdetails = ConsoleHelper.GetBinaryClassificationMetrics(trainer.ToString(), metrics);
        metricdetails.Prepend(DateTime.UtcNow.ToLongDateString());
        File.WriteAllLines(metricPath, metricdetails);
        System.Console.WriteLine("Completed printing model metrics");
        
    }

    public async Task SaveModel()
    {
        System.Console.WriteLine("Saving trained model");
        mlContext.Model.Save(trainedModel, trainingData.Schema, modelPath);
        await modelRepo.UploadBlobStore(modelPath, metricPath);
        // await modelRepo.CreateZip(modelPath);
        // await modelRepo.CreateModelRelease(modelPath);
    }

}