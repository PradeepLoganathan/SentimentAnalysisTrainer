
using Octokit;
using System;
using System.Threading.Tasks;

namespace Common;
public class Github
{
    private GitHubClient _client;
    private string _owner;
    private string _repoName;
    private static string APPNAME = "ModelRepositoryApp"

    public Github(string username, string password)
    {
        // Initialize the client and credentials
        _client = new GitHubClient(new ProductHeaderValue(APPNAME));
        _client.Credentials = new Credentials(username, password);
    }

     public Github(string token)
    {
        // Initialize the client and credentials
        _client = new GitHubClient(new ProductHeaderValue(APPNAME));
        _client.Credentials = new Credentials(token);
    }

    public void SetRepo(string owner, string repoName)
    {
        // Set the owner and repo name
        _owner = owner;
        _repoName = repoName;
    }

    public async Task CreateFile(string filePath, string fileContent,
        string commitMessage)
    {
        try
        {
            // Create a file in the repo
            await _client.Repository.Content.CreateFile(_owner,
                _repoName,
                filePath,
                new CreateFileRequest(commitMessage, fileContent));
            // Log success message
            Console.WriteLine($"Created file {filePath} in {_owner}/{_repoName}.");
        }
        catch (Exception ex)
        {
            // Log error message
            Console.WriteLine($"Failed to create file {filePath} in {_owner}/{_repoName}. Error: {ex.Message}");
            // Rethrow exception
            throw;
        }
    }

    public async Task UpdateFile(string filePath, string fileContent,
        string commitMessage)
    {
        try
        {
            // Get the SHA of the previous commit
            var result = await _client.Repository.Content.GetAllContents(_owner,
                _repoName,
                filePath);
            var sha = result[0].Sha;
            // Update the file in the repo
            await _client.Repository.Content.UpdateFile(_owner,
                _repoName,
                filePath,
                new UpdateFileRequest(commitMessage, fileContent , sha));
            // Log success message
            Console.WriteLine($"Updated file {filePath} in {_owner}/{_repoName}.");
        }
        catch (Exception ex)
        {
            // Log error message
            Console.WriteLine($"Failed to update file {filePath} in {_owner}/{_repoName}. Error: {ex.Message}");
            // Rethrow exception
            throw;
        }
    }

    public async Task<string> ReadFile(string filePath)
    {

       try 
       {
           // Get the content of the file from the repo
           var result = await _client.Repository.Content.GetAllContents(_owner,
               _repoName,
               filePath);
           var content = result[0].Content;
           // Log success message 
           Console.WriteLine($"Read file {filePath} from {_owner}/{_repoName}.");
           return content; 
       } 
       catch (Exception ex) 
       {
           // Log error message 
           Console.WriteLine($"Failed to read file {filePath} from {_owner}/{_repoName}. Error: {ex.Message}");
           // Rethrow exception 
           throw; 
       }
    
   }

   public async Task DeleteFile(string filePath, string commitMessage)
   {
       try
       {
           // Get the SHA of the previous commit
           var result = await _client.Repository.Content.GetAllContents(_owner,
               _repoName,
               filePath);
           var sha = result[0].Commit.Sha;
           // Delete the file from the repo
           await _client.Repository.Content.DeleteFile(_owner,
               _repoName,
               filePath,
               new DeleteFileRequest(commitMessage, sha));
           // Log success message
           Console.WriteLine($"Deleted file {filePath} from {_owner}/{_repoName}.");
       }
       catch (Exception ex)
       {
          // Log error message 
          Console.WriteLine($"Failed to delete file {filePath} from {_owner}/{_repoName}. Error: {ex.Message}");
           // Rethrow exception 
           throw; 
       }
    
   }
}