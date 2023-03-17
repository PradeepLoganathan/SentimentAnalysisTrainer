
using Octokit;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using Azure.Storage.Blobs;
using System.IO.Compression;


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

    public async Task UploadBlobStore(string filePath)
    {
        string connectionString = "DefaultEndpointsProtocol=https;AccountName=modelrepositoryprod;AccountKey=/oRe/ytUUWwonN3kZ9YtPytHKCav+SizB6516u/6We9yxOU9cz3Z7ZforJE6OUssYMW4eQItB09h+AStk38oVA==;EndpointSuffix=core.windows.net";
        string containerName = "modelrepositoryprod";
        string blobName = "sentimentanalysismodel";

        BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        using (FileStream zipStream = new FileStream(filePath, System.IO.FileMode.Open))
        {
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(zipStream, true);
        }
    }

    public async Task CreateZip(string filePath)
    {
        try
        {
            var zipFile = File.ReadAllBytes(filePath);
            var base64String = Convert.ToBase64String(zipFile);

            var newBlob = new NewBlob
            {
                Content = base64String,
                Encoding = EncodingType.Base64
            };


            var blob = await _client.Git.Blob.Create(_owner, _repoName, newBlob);
            // var branchRef = await _client.Git.Reference.Get(_owner, _repoName, $"heads/{_branch}");
            // var latestCommit = await _client.Git.Commit.Get(_owner, _repoName, branchRef.Object.Sha);


            var newTree = new NewTree();
            newTree.Tree.Add(new NewTreeItem
            {
                Type = TreeType.Blob,
                Mode = Octokit.FileMode.File,
                Path = "SentimentAnalysisModel.zip",
                Sha = blob.Sha
            });


            // Create a tree using this request 
            var createdTree =
                await _client.Git.Tree.Create(_owner, _repoName, newTree);

            var master = await _client.Git.Reference
            .Get(_owner, _repoName, "heads/master");

            var newCommit = new NewCommit(
                        "Hello World!",
                        createdTree.Sha,
                        new[] { master.Object.Sha })
            { Author = new Committer("PradeepLoganathan", "mytestemail@ztc.xom", DateTime.UtcNow) };

            var createdCommit = await _client.Git.Commit
            .Create(_owner, _repoName, newCommit);

            var updateReference = new ReferenceUpdate(createdCommit.Sha);
            var updatedReference = await _client.Git.Reference.Update(_owner, _repoName, "heads/master", updateReference);
        }
        catch (System.Exception e)
        {
            Console.WriteLine(e.ToString());
            throw;
        }


    }

    public async Task CreateModelRelease(string modelPath)
    {
        try
        {
            using (var archiveContents = File.OpenRead(modelPath))
            {
                var assetUpload = new ReleaseAssetUpload()
                {
                    FileName = modelPath,
                    ContentType = "application/zip",
                    RawData = archiveContents
                };

                var releases = await _client.Repository.Release.GetAll(_owner, _repoName);
                NewRelease newRelease;

                if (releases.Count == 0)
                {
                    newRelease = new NewRelease("v1.0.0");
                    newRelease.Name = "Version One Point Oh";
                    newRelease.Body = "This is the first release of the model";
                    newRelease.Draft = false;
                    newRelease.Prerelease = false;

                }
                else
                {
                    var latest = releases[0].TagName;
                    string[] versionParts = latest.Split('.');
                    int major = int.Parse(versionParts[0].Substring(1));
                    int minor = int.Parse(versionParts[1]);
                    int build = int.Parse(versionParts[2]);
                    build++;
                    string newVersion = $"v{major}.{minor}.{build}";

                    string[] numbers = new string[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };
                    string majorWord = numbers[major];
                    string minorWord = numbers[minor];
                    string buildWord = numbers[build];

                    string versionWord = $"v{majorWord}.{minorWord}.{buildWord}";

                    newRelease = new NewRelease(newVersion);
                    newRelease.Name = versionWord;
                    newRelease.Body = "This is the {versionWord} release of the model";
                    newRelease.Draft = false;
                    newRelease.Prerelease = false;

                }


                var release = await _client.Repository.Release.Create(_owner, _repoName, newRelease);
                var asset = await _client.Repository.Release.UploadAsset(release, assetUpload);
            }
        }
        catch (Octokit.ApiValidationException octovalException)
        {
            Console.WriteLine(octovalException.ToString());
            throw;
        }
        catch (System.Exception)
        {

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