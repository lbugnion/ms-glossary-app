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
    public class GitHubHelper
    {
        private const string CommitUrl = "git/commits";
        private const string CreateTreeUrl = "git/trees";
        private const string GetHeadUrl = "git/ref/heads/master";
        private const string GitHubApiBaseUrlMask = "https://api.github.com/repos/{0}/{1}/{2}";
        private const string UpdateReferenceUrl = "git/refs/heads/master";
        private const string UploadBlobUrl = "git/blobs";

        // See http://www.levibotelho.com/development/commit-a-file-with-the-github-api/

        public async Task<string> CommitFiles(
            string accountName,
            string repoName,
            string githubToken,
            string commitMessage,
            IList<(string path, string content)> fileNamesAndContent,
            ILogger log = null)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "MsGlossaryApp");

            var url = string.Format(
                GitHubApiBaseUrlMask,
                accountName,
                repoName,
                GetHeadUrl);

            log?.LogInformation($"repoName: {repoName}");
            log?.LogInformation($"url: {url}");

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get
            };

            var response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    var errorMessage = $"Error getting heads: {await response.Content.ReadAsStringAsync()}";
                    log?.LogInformation(errorMessage);
                    return errorMessage;
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Unknown error getting heads: {ex.Message}";
                    log?.LogInformation(errorMessage);
                    return errorMessage;
                }
            }

            var jsonResult = await response.Content.ReadAsStringAsync();
            var mainHead = JsonConvert.DeserializeObject<GetHeadResult>(jsonResult);
            log?.LogInformation($"Found main head");

            // Grab main commit

            log?.LogInformation("Grabbing main commit");

            request = new HttpRequestMessage
            {
                RequestUri = new Uri(mainHead.Object.Url),
                Method = HttpMethod.Get
            };

            response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    var errorMessage = $"Error getting commit: {await response.Content.ReadAsStringAsync()}";
                    log?.LogInformation(errorMessage);
                    return errorMessage;
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Unknown error getting commit: {ex.Message}";
                    log?.LogInformation(errorMessage);
                    return errorMessage;
                }
            }

            jsonResult = await response.Content.ReadAsStringAsync();
            var masterCommitResult = JsonConvert.DeserializeObject<CommitResult>(jsonResult);
            log?.LogInformation($"Done grabbing master commit {masterCommitResult.Sha}");

            // Post new file(s) to GitHub blob

            var treeInfos = new List<TreeInfo>();
            string jsonRequest;

            foreach (var file in fileNamesAndContent)
            {
                log?.LogInformation($"Posting to GitHub blob: {file.path}");
                var uploadInfo = new UploadInfo
                {
                    Content = file.content
                };

                jsonRequest = JsonConvert.SerializeObject(uploadInfo);

                url = string.Format(
                    GitHubApiBaseUrlMask,
                    accountName,
                    repoName,
                    UploadBlobUrl);

                request = new HttpRequestMessage
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Post,
                    Content = new StringContent(jsonRequest)
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);

                response = await client.SendAsync(request);

                if (response.StatusCode != HttpStatusCode.Created)
                {
                    try
                    {
                        var errorMessage = $"Error uploading blob: {await response.Content.ReadAsStringAsync()}";
                        log?.LogInformation(errorMessage);
                        return errorMessage;
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = $"Unknown error uploading blob: {ex.Message}";
                        log?.LogInformation(errorMessage);
                        return errorMessage;
                    }
                }

                jsonResult = await response.Content.ReadAsStringAsync();
                var uploadBlobResult = JsonConvert.DeserializeObject<ShaInfo>(jsonResult);
                log?.LogInformation($"Done posting to GitHub blob {uploadBlobResult.Sha}");

                var info = new TreeInfo(file.path, uploadBlobResult.Sha);
                treeInfos.Add(info);
            }

            // Create the tree

            log?.LogInformation("Creating the tree");
            var newTreeInfo = new CreateTreeInfo()
            {
                BaseTree = masterCommitResult.Tree.Sha,
            };

            newTreeInfo.AddTreeInfos(treeInfos);

            jsonRequest = JsonConvert.SerializeObject(newTreeInfo);

            url = string.Format(
                GitHubApiBaseUrlMask,
                accountName,
                repoName,
                CreateTreeUrl);

            request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Content = new StringContent(jsonRequest)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);

            response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                try
                {
                    var message = $"Error creating tree: {await response.Content.ReadAsStringAsync()}";
                    log?.LogInformation(message);
                    return message;
                }
                catch (Exception ex)
                {
                    var message = $"Unknown error creating tree: {ex.Message}";
                    log?.LogInformation(message);
                    return message;
                }
            }

            jsonResult = await response.Content.ReadAsStringAsync();
            var createTreeResult = JsonConvert.DeserializeObject<ShaInfo>(jsonResult);
            log?.LogInformation($"Done creating the tree {createTreeResult.Sha}");

            // Create the commit

            log?.LogInformation("Creating the commit");
            var commitInfo = new CommitInfo(
                commitMessage,
                masterCommitResult.Sha,
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

            response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                try
                {
                    var message = $"Error creating commit: {await response.Content.ReadAsStringAsync()}";
                    log?.LogInformation(message);
                    return message;
                }
                catch (Exception ex)
                {
                    var message = $"Unknown error creating commit: {ex.Message}";
                    log?.LogInformation(message);
                    return message;
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
                UpdateReferenceUrl);

            request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Patch,
                Content = new StringContent(jsonRequest)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);

            response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    var message = $"Error updating reference: {await response.Content.ReadAsStringAsync()}";
                    log?.LogInformation(message);
                    return message;
                }
                catch (Exception ex)
                {
                    var message = $"Unknown error updating reference: {ex.Message}";
                    log?.LogInformation(message);
                    return message;
                }
            }

            jsonResult = await response.Content.ReadAsStringAsync();
            var headResult = JsonConvert.DeserializeObject<GetHeadResult>(jsonResult);
            log?.LogInformation("Done updating the reference");
            log?.LogInformation($"Ref: {headResult.Ref}");

            return string.Empty;
        }
    }
}