using System.Threading.Tasks;
using System.Linq;
using System.IO;

using Common;
using DataStructures;
using Microsoft.ML;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms.Text;
using Repository;
using static Microsoft.ML.DataOperationsCatalog;
using System;

namespace ModelTraining;

internal class ModelBuilder
{
    MLContext mlContext;
  
    string dataFilePath = "wikiDetoxAnnotated40kRows.tsv";
    string modelPath = "SentimentModel.zip";
    string metricPath = "SentimentModel-Metrics.txt";
    IDataView dataView, trainingData, testData;
    ITransformer trainedModel;
    ModelRepo modelRepo;
    DataRepo dataRepo;
    TextFeaturizingEstimator dataProcessPipeline;
    SdcaLogisticRegressionBinaryTrainer trainer;

    public ModelBuilder()
    {
        mlContext = new MLContext(seed: 1);
        dataRepo = new DataRepo();
        modelRepo = new ModelRepo();
    }

    public async Task LoadTrainingData()
    {
        await dataRepo.GetTrainingData(dataFilePath);
        dataView = mlContext.Data.LoadFromTextFile<SentimentIssue>(dataFilePath, hasHeader: true);
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
        System.Console.WriteLine("Saving trained");
        mlContext.Model.Save(trainedModel, trainingData.Schema, modelPath);
        await modelRepo.UploadModelVersion(modelPath);
        await modelRepo.UploadMetricVersion(metricPath);
    }

}