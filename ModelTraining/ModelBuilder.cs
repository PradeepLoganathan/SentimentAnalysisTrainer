using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Common;
using DataStructures;
using Microsoft.ML;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms.Text;
using ModelRepository;
using Octokit;
using static Microsoft.ML.DataOperationsCatalog;

namespace ModelTraining;

internal class ModelBuilder
{
    MLContext mlContext;
    string wikiDetoxRepoPath = @"Data/wikiDetoxAnnotated40kRows.tsv";
    string wikiDetoxLocalFilePath = "wikiDetoxAnnotated40kRows.tsv";
    string modelPath = "SentimentModel.zip";
    IDataView dataView;
    IDataView trainingData;
    IDataView testData;
    ITransformer trainedModel;

    Github trainingDataRepo, modelRepo;

    private TextFeaturizingEstimator dataProcessPipeline;

    public ModelBuilder(Github TrainingDataRepo, Github ModelRepo)
    {
        trainingDataRepo = TrainingDataRepo;
        modelRepo = ModelRepo;
        mlContext = new MLContext(seed: 1);
    }

    public async Task LoadTrainingData()
    {

        await trainingDataRepo.ReadFile(wikiDetoxRepoPath, wikiDetoxLocalFilePath);
        //File.WriteAllText(wikiDetoxFilePath, contents);

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
        // Select algorithm and configure model builder                            
        var trainer = mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features");
        var trainingPipeline = dataProcessPipeline.Append(trainer);

        // Train the model
        trainedModel = trainingPipeline.Fit(trainingData);
        EvaluateModel(mlContext, testData, trainer, trainedModel);
    }

    public void EvaluateModel(MLContext mlContext, IDataView testData, SdcaLogisticRegressionBinaryTrainer trainer, ITransformer trainedModel)
    {
        // Evaluate the model
        var predictions = trainedModel.Transform(testData);
        var metrics = mlContext.BinaryClassification.Evaluate(data: predictions, labelColumnName: "Label", scoreColumnName: "Score");

        // Display model accuracy stats
        ConsoleHelper.PrintBinaryClassificationMetrics(trainer.ToString(), metrics);
    }

    public async Task SaveModel()
    {
        //mlContext.Model.Save(trainedModel, trainingData.Schema, modelPath);
        // var content = File.ReadAllText(ModelPath);
        // await ghClient.Repository.Content.CreateFile(owner, name, path, new CreateFileRequest("SentimentAnalysis model updated", content));

        // Console.WriteLine("The model is saved to {0}", ModelPath);

        mlContext.Model.Save(trainedModel, trainingData.Schema, modelPath);
        await modelRepo.CreateZip(modelPath);
    }

}