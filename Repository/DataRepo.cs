using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;

namespace Repository;
public class DataRepo
{
    string trainingDataRepo;
    public DataRepo(string TrainingDataRepo = "https://raw.githubusercontent.com/PradeepLoganathan/TrainingDataRepository/main/SentimentAnalysis/wikiDetoxAnnotated40kRows.tsv")
    {
        trainingDataRepo = TrainingDataRepo;
    }

    public async Task GetTrainingData(string localFilePath)
    {
        try
        {
            Console.WriteLine("Getting training data...");
            var contents = await new HttpClient()
                .GetStringAsync(trainingDataRepo);
        
            File.WriteAllText(localFilePath, contents);
            Console.WriteLine("Training data accquired...");
            
        }
        catch (Exception ex)
        {
            
            Console.WriteLine($"Failed to get training data" + ex.ToString());
            throw;
        }
    }

}