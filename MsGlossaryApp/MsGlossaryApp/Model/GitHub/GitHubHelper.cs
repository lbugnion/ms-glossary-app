using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
        private const string GetMarkdownFileUrl = "contents/{0}?ref={1}";
        private const string GitHubApiBaseUrlMask = "https://api.github.com/repos/{0}/{1}/{2}";
        private const string UpdateReferenceUrl = "git/refs/heads/{0}";
        private const string UploadBlobUrl = "git/blobs";
        private readonly HttpClient _client;

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
            log?.LogInformation("In GitHubHelper.CommitFiles");

            if (existingBranchInfo == null)
            {
                existingBranchInfo = await GetHead(
                    accountName,
                    repoName,
                    branchName,
                    githubToken,
                    log);

                if (!string.IsNullOrEmpty(existingBranchInfo.ErrorMessage))
                {
                    return new GetHeadResult
                    {
                        ErrorMessage = existingBranchInfo.ErrorMessage
                    };
                }
            }

            var mainCommit = await GetMainCommit(existingBranchInfo, githubToken, log);

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

            foreach (var (path, content) in fileNamesAndContent)
            {
                log?.LogDebug($"Posting to GitHub blob: {path}");
                var uploadInfo = new UploadInfo
                {
                    Content = content
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
                        log?.LogError(errorMessage);
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
                log?.LogInformation($"Done posting to GitHub blob {uploadBlobResult.Sha}");

                var info = new TreeInfo(path, uploadBlobResult.Sha);
                treeInfos.Add(info);
            }

            // Create the tree

            log?.LogInformation("Creating the tree");
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
                    log?.LogError(message);
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
            log?.LogInformation($"Done creating the tree {createTreeResult.Sha}");

            // Create the commit

            log?.LogInformation("Creating the commit");
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
                    log?.LogError(message);
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
            log?.LogInformation($"Done creating the commit {createCommitResult.Sha}");

            // Update reference

            log?.LogInformation("Updating the reference");
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
                    log?.LogError(message);
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
            log?.LogInformation("Done updating the reference");
            log?.LogDebug($"Ref: {headResult.Ref}");

            log?.LogInformation("Out GitHubHelper.CommitFiles");

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
            log?.LogInformation("In GitHubHelper.CreateNewBranch");

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
                log?.LogError($"Error when creating new branch: {newBranchName} / {errorResult.ErrorMessage}");
                return new GetHeadResult
                {
                    ErrorMessage = $"Error when creating new branch: {newBranchName} / {errorResult.ErrorMessage}"
                };
            }

            var jsonResult = await response.Content.ReadAsStringAsync();
            var createNewBranchResult = JsonConvert.DeserializeObject<GetHeadResult>(jsonResult);
            log?.LogDebug($"Done creating new branch {createNewBranchResult.Object.Sha}");

            log?.LogInformation("Out GitHubHelper.CreateNewBranch");

            return createNewBranchResult;
        }

        public async Task<GetHeadResult> GetHead(
            string accountName,
            string repoName,
            string branchName,
            string token,
            ILogger log = null)
        {
            log?.LogInformation("In GitHubHelper.GetHead");

            var url = string.Format(
                GitHubApiBaseUrlMask,
                accountName,
                repoName,
                string.Format(GetHeadUrl, branchName));

            log?.LogDebug($"repoName: {repoName}");
            log?.LogDebug($"url: {url}");

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get,
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    var errorMessage = $"Error getting head for {branchName}: {await response.Content.ReadAsStringAsync()}";
                    log?.LogError(errorMessage);
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
            log?.LogDebug($"Found head for {branchName}");

            log?.LogInformation("Out GitHubHelper.GetHead");

            return mainHead;
        }

        public async Task<CommitResult> GetMainCommit(
            GetHeadResult branchHead,
            string githubToken,
            ILogger log = null)
        {
            // Grab main commit

            log?.LogInformation("In GitHubHelper.GetMainCommit");

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(branchHead.Object.Url),
                Method = HttpMethod.Get
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);

            var response = await _client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    var errorMessage = $"Error getting commit: {await response.Content.ReadAsStringAsync()}";
                    log?.LogError(errorMessage);
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
            log?.LogDebug($"Done grabbing master commit {masterCommitResult.Sha}");

            log?.LogInformation("Out GitHubHelper.GetMainCommit");

            return masterCommitResult;
        }

        public async Task<GetTextFileResult> GetTextFile(
                                            string accountName,
            string repoName,
            string branchName,
            string filePathWithExtension,
            string githubToken,
            ILogger log = null)
        {
            // TODO Add logging

            var getFileUrl = string.Format(GetMarkdownFileUrl, filePathWithExtension, branchName);
            var url = string.Format(GitHubApiBaseUrlMask, accountName, repoName, getFileUrl);

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
            var response = await _client.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return new GetTextFileResult
                {
                    ErrorMessage = responseText
                };
            }

            try
            {
                var result = JsonConvert.DeserializeObject<GetTextFileResult>(responseText);

                if (result.Type != "file")
                {
                    return new GetTextFileResult
                    {
                        ErrorMessage = $"{filePathWithExtension} doesn't seem to be a file on GitHub"
                    };
                }

                if (string.IsNullOrEmpty(result.EncodedContent))
                {
                    return new GetTextFileResult
                    {
                        ErrorMessage = $"{filePathWithExtension} doesn't have content"
                    };
                }

                var bytes = Convert.FromBase64String(result.EncodedContent);
                result.TextContent = Encoding.UTF8.GetString(bytes);
                return result;
            }
            catch (Exception ex)
            {
                return new GetTextFileResult
                {
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}