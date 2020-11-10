using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MsGlossaryApp.Model.GitHub
{
    // See http://www.levibotelho.com/development/commit-a-file-with-the-github-api/
    public class GitHubHelper
    {
        private const string CommitUrl = "git/commits";
        private const string CreateNewBranchUrl = "git/refs";
        private const string CreateTreeUrl = "git/trees";
        private const string GetHeadUrl = "git/ref/heads/{0}";
        private const string GitHubApiBaseUrlMask = "https://api.github.com/repos/{0}/{1}/{2}";
        private const string UpdateReferenceUrl = "git/refs/heads/{0}";
        private const string UploadBlobUrl = "git/blobs";

        private HttpClient _client;

        public GitHubHelper()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("User-Agent", "GalaSoft.GitHubHelper");
        }

        public GitHubHelper(HttpClient client)
        {
            _client = client;
        }

        public async Task<GetHeadResult> CommitFiles(
            string accountName,
            string repoName,
            string branchName,
            string githubToken,
            string commitMessage,
            IList<(string path, string content)> fileNamesAndContent,
            GetHeadResult existingBranchInfo = null,
            ILogger log = null)
        {
            log?.LogInformationEx("In GitHubHelper.CommitFiles", LogVerbosity.Verbose);

            if (existingBranchInfo == null)
            {
                existingBranchInfo = await GetHead(
                    accountName,
                    repoName,
                    branchName,
                    log);

                if (!string.IsNullOrEmpty(existingBranchInfo.ErrorMessage))
                {
                    return new GetHeadResult
                    {
                        ErrorMessage = existingBranchInfo.ErrorMessage
                    };
                }
            }

            var mainCommit = await GetMainCommit(existingBranchInfo, log);

            if (!string.IsNullOrEmpty(mainCommit.ErrorMessage))
            {
                return new GetHeadResult
                {
                    ErrorMessage = mainCommit.ErrorMessage
                };
            }

            // Post new file(s) to GitHub blob

            var treeInfos = new List<TreeInfo>();
            string jsonRequest;

            foreach (var file in fileNamesAndContent)
            {
                log?.LogInformationEx($"Posting to GitHub blob: {file.path}", LogVerbosity.Verbose);
                var uploadInfo = new UploadInfo
                {
                    Content = file.content
                };

                jsonRequest = JsonConvert.SerializeObject(uploadInfo);

                var uploadBlobUrl = string.Format(
                    GitHubApiBaseUrlMask,
                    accountName,
                    repoName,
                    UploadBlobUrl);

                var uploadBlobRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(uploadBlobUrl),
                    Method = HttpMethod.Post,
                    Content = new StringContent(jsonRequest)
                };

                uploadBlobRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);

                var uploadBlobResponse = await _client.SendAsync(uploadBlobRequest);

                if (uploadBlobResponse.StatusCode != HttpStatusCode.Created)
                {
                    try
                    {
                        var errorMessage = $"Error uploading blob: {await uploadBlobResponse.Content.ReadAsStringAsync()}";
                        log?.LogInformationEx(errorMessage, LogVerbosity.Verbose);
                        return new GetHeadResult
                        {
                            ErrorMessage = errorMessage
                        };
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = $"Unknown error uploading blob: {ex.Message}";
                        log?.LogError(errorMessage);
                        return new GetHeadResult
                        {
                            ErrorMessage = errorMessage
                        };
                    }
                }

                var uploadBlobJsonResult = await uploadBlobResponse.Content.ReadAsStringAsync();
                var uploadBlobResult = JsonConvert.DeserializeObject<ShaInfo>(uploadBlobJsonResult);
                log?.LogInformationEx($"Done posting to GitHub blob {uploadBlobResult.Sha}", LogVerbosity.Verbose);

                var info = new TreeInfo(file.path, uploadBlobResult.Sha);
                treeInfos.Add(info);
            }

            // Create the tree

            log?.LogInformationEx("Creating the tree", LogVerbosity.Verbose);
            var newTreeInfo = new CreateTreeInfo()
            {
                BaseTree = mainCommit.Tree.Sha,
            };

            newTreeInfo.AddTreeInfos(treeInfos);

            jsonRequest = JsonConvert.SerializeObject(newTreeInfo);

            var url = string.Format(
                GitHubApiBaseUrlMask,
                accountName,
                repoName,
                CreateTreeUrl);

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Content = new StringContent(jsonRequest)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);

            var response = await _client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                try
                {
                    var message = $"Error creating tree: {await response.Content.ReadAsStringAsync()}";
                    log?.LogInformationEx(message, LogVerbosity.Verbose);
                    return new GetHeadResult
                    {
                        ErrorMessage = message
                    };
                }
                catch (Exception ex)
                {
                    var message = $"Unknown error creating tree: {ex.Message}";
                    log?.LogError(message);
                    return new GetHeadResult
                    {
                        ErrorMessage = message
                    };
                }
            }

            var jsonResult = await response.Content.ReadAsStringAsync();
            var createTreeResult = JsonConvert.DeserializeObject<ShaInfo>(jsonResult);
            log?.LogInformationEx($"Done creating the tree {createTreeResult.Sha}", LogVerbosity.Verbose);

            // Create the commit

            log?.LogInformationEx("Creating the commit", LogVerbosity.Verbose);
            var commitInfo = new CommitInfo(
                commitMessage,
                mainCommit.Sha,
                createTreeResult.Sha);

            jsonRequest = JsonConvert.SerializeObject(commitInfo);

            url = string.Format(
                GitHubApiBaseUrlMask,
                accountName,
                repoName,
                CommitUrl);

            request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Content = new StringContent(jsonRequest)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);

            response = await _client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                try
                {
                    var message = $"Error creating commit: {await response.Content.ReadAsStringAsync()}";
                    log?.LogInformationEx(message, LogVerbosity.Verbose);
                    return new GetHeadResult
                    {
                        ErrorMessage = message
                    };
                }
                catch (Exception ex)
                {
                    var message = $"Unknown error creating commit: {ex.Message}";
                    log?.LogError(message);
                    return new GetHeadResult
                    {
                        ErrorMessage = message
                    };
                }
            }

            jsonResult = await response.Content.ReadAsStringAsync();
            var createCommitResult = JsonConvert.DeserializeObject<ShaInfo>(jsonResult);
            log?.LogInformationEx($"Done creating the commit {createCommitResult.Sha}", LogVerbosity.Verbose);

            // Update reference

            log?.LogInformationEx("Updating the reference", LogVerbosity.Verbose);
            var updateReferenceInfo = new UpdateReferenceInfo(createCommitResult.Sha);

            jsonRequest = JsonConvert.SerializeObject(updateReferenceInfo);

            url = string.Format(
                GitHubApiBaseUrlMask,
                accountName,
                repoName,
                string.Format(UpdateReferenceUrl, branchName));

            request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Patch,
                Content = new StringContent(jsonRequest)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);

            response = await _client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    var message = $"Error updating reference: {await response.Content.ReadAsStringAsync()}";
                    log?.LogInformationEx(message, LogVerbosity.Verbose);
                    return new GetHeadResult
                    {
                        ErrorMessage = message
                    };
                }
                catch (Exception ex)
                {
                    var message = $"Unknown error updating reference: {ex.Message}";
                    log?.LogError(message);
                    return new GetHeadResult
                    {
                        ErrorMessage = message
                    };
                }
            }

            jsonResult = await response.Content.ReadAsStringAsync();
            var headResult = JsonConvert.DeserializeObject<GetHeadResult>(jsonResult);
            log?.LogInformationEx("Done updating the reference", LogVerbosity.Verbose);
            log?.LogInformationEx($"Ref: {headResult.Ref}", LogVerbosity.Debug);

            log?.LogInformationEx("Out GitHubHelper.CommitFiles", LogVerbosity.Verbose);

            return headResult;
        }

        public async Task<GetHeadResult> CreateNewBranch(
            string accountName,
            string repoName,
            string token,
            GetHeadResult mainHead,
            string newBranchName = null,
            ILogger log = null)
        {
            log?.LogInformationEx("In GitHubHelper.CreateNewBranch", LogVerbosity.Verbose);

            var newBranchRequestBody = new NewBranchInfo
            {
                Sha = mainHead.Object.Sha,
                Ref = string.Format(NewBranchInfo.RefMask, newBranchName)
            };

            var jsonRequest = JsonConvert.SerializeObject(newBranchRequestBody);

            var url = string.Format(GitHubApiBaseUrlMask, accountName, repoName, CreateNewBranchUrl);
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Content = new StringContent(jsonRequest)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                var errorResultJson = await response.Content.ReadAsStringAsync();
                var errorResult = JsonConvert.DeserializeObject<ErrorResult>(errorResultJson);
                log?.LogInformationEx($"Error when creating new branch: {newBranchName} / {errorResult.Message}", LogVerbosity.Verbose);
                return new GetHeadResult
                {
                    ErrorMessage = $"Error when creating new branch: {newBranchName} / {errorResult.Message}"
                };
            }

            var jsonResult = await response.Content.ReadAsStringAsync();
            var createNewBranchResult = JsonConvert.DeserializeObject<GetHeadResult>(jsonResult);
            log?.LogInformationEx($"Done creating new branch {createNewBranchResult.Object.Sha}", LogVerbosity.Verbose);

            log?.LogInformationEx("Out GitHubHelper.CreateNewBranch", LogVerbosity.Verbose);

            return createNewBranchResult;
        }

        public async Task<GetHeadResult> GetHead(
            string accountName,
            string repoName,
            string branchName,
            ILogger log = null)
        {
            log?.LogInformationEx("In GitHubHelper.GetHead", LogVerbosity.Verbose);

            var url = string.Format(
                GitHubApiBaseUrlMask,
                accountName,
                repoName,
                string.Format(GetHeadUrl, branchName));

            log?.LogInformationEx($"repoName: {repoName}", LogVerbosity.Debug);
            log?.LogInformationEx($"url: {url}", LogVerbosity.Debug);

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get
            };

            var response = await _client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    var errorMessage = $"Error getting head for {branchName}: {await response.Content.ReadAsStringAsync()}";
                    log?.LogInformationEx(errorMessage, LogVerbosity.Verbose);
                    return new GetHeadResult
                    {
                        ErrorMessage = errorMessage
                    };
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Unknown error getting head for {branchName}: {ex.Message}";
                    log?.LogError(errorMessage);
                    return new GetHeadResult
                    {
                        ErrorMessage = errorMessage
                    };
                }
            }

            var jsonResult = await response.Content.ReadAsStringAsync();
            var mainHead = JsonConvert.DeserializeObject<GetHeadResult>(jsonResult);
            log?.LogInformationEx($"Found head for {branchName}", LogVerbosity.Verbose);

            log?.LogInformationEx("Out GitHubHelper.GetHead", LogVerbosity.Verbose);

            return mainHead;
        }

        public async Task<CommitResult> GetMainCommit(
                            GetHeadResult branchHead,
            ILogger log = null)
        {
            // Grab main commit

            log?.LogInformationEx("In GitHubHelper.GetMainCommit", LogVerbosity.Verbose);

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(branchHead.Object.Url),
                Method = HttpMethod.Get
            };

            var response = await _client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    var errorMessage = $"Error getting commit: {await response.Content.ReadAsStringAsync()}";
                    log?.LogInformationEx(errorMessage, LogVerbosity.Verbose);
                    return new CommitResult
                    {
                        ErrorMessage = errorMessage
                    };
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Unknown error getting commit: {ex.Message}";
                    log?.LogError(errorMessage);
                    return new CommitResult
                    {
                        ErrorMessage = errorMessage
                    };
                }
            }

            var jsonResult = await response.Content.ReadAsStringAsync();
            var masterCommitResult = JsonConvert.DeserializeObject<CommitResult>(jsonResult);
            log?.LogInformationEx($"Done grabbing master commit {masterCommitResult.Sha}", LogVerbosity.Debug);

            log?.LogInformationEx("Out GitHubHelper.GetMainCommit", LogVerbosity.Verbose);

            return masterCommitResult;
        }
    }
}