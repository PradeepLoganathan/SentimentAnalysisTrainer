
using Octokit;
using Octokit.Models.Response;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Encodings;
using System.Collections.Generic;
using System.Net.Http;

namespace ModelRepository;
public class Github
{
    private GitHubClient _client;
    private string _owner;
    private string _repoName;
    private string _branch;
    private static string APPNAME = "ModelRepositoryApp";

    public Github(string username, string password) : this()
    {
        // Initialize the client and credentials
        _client = new GitHubClient(new ProductHeaderValue(APPNAME));
        _client.Credentials = new Credentials(username, password);
    }

    public Github(string token) : this()
    {
        // Initialize the client and credentials
        _client = new GitHubClient(new ProductHeaderValue(APPNAME));
        _client.Credentials = new Credentials(token);
    }

    public Github()
    {
        _branch = "master";
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

    public async Task CreateZip(string filePath)
    {
        var zipFile = File.ReadAllBytes(filePath);
        var base64String = Convert.ToBase64String(zipFile);

        var newBlob = new NewBlob
        {
            Content = base64String,
            Encoding = EncodingType.Base64
        };


        var blob = await _client.Git.Blob.Create(_owner, _repoName, newBlob);
        var branchRef = await _client.Git.Reference.Get(_owner, _repoName, $"heads/{_branch}");
        var latestCommit = await _client.Git.Commit.Get(_owner, _repoName, branchRef.Object.Sha);


        var newTree = new NewTree();
        newTree.Tree.Add(new NewTreeItem
        {
            Type = TreeType.Blob,
            Mode = Octokit.FileMode.File,
            Path = "SentimentAnalysisModel.zip",
            Sha = blob.Sha
        });


        // Create a tree using this request 
        var treeResponse =
            await _client.Git.Tree.Create(_owner, _repoName, newTree);

        // Create a commit for this tree and set its parent as current commit 
        var commitRequest =
            new NewCommit("Add my-zip-file.zip",
                          treeResponse.Sha,
                          latestCommit.Sha);

        // Create this commit 
        var commit =
            await _client.Git.Commit.Create(_owner,
                                           _repoName,
                                           commitRequest);

        // Update HEAD with this commit 
        var referece = await _client.Git.Reference.Update(_owner,
                                           _repoName,
                                           $"heads/{_branch}",
                                           new ReferenceUpdate(commit.Sha));

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
                new UpdateFileRequest(commitMessage, fileContent, sha));
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

    public async Task ReadFile(string repoFilePath, string localFilePath)
    {

        try
        {
            // Get the content of the file from the repo
            var contents = await _client.Repository.Content.GetAllContents(_owner,
                _repoName,
                repoFilePath);
            var downloadurl = contents[0].DownloadUrl;

            using (var client = new HttpClient())
            {
                using (var response = await client.GetAsync(downloadurl))
                {
                    using (var content = response.Content)
                    {
                        using (var stream = await content.ReadAsStreamAsync())
                        {
                            using (var fileStream = new FileStream(localFilePath, System.IO.FileMode.Create, FileAccess.Write))
                            {
                                stream.CopyTo(fileStream);
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"Read file {localFilePath} from {_owner}/{_repoName}.");
            
        }
        catch (Exception ex)
        {
            // Log error message 
            Console.WriteLine($"Failed to read file {localFilePath} from {_owner}/{_repoName}. Error: {ex.Message}");
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
            var sha = result[0].Sha;
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