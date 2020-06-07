using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using WordsOfTheDayApp.Model;
using WordsOfTheDayApp.Model.NewTopic;

namespace WordsOfTheDayApp
{
    public static class AddSynopsis
    {
        private const string ApiBaseUrl = "https://api.github.com/repos/lbugnion/{0}/{1}";
        private const string CommitMessage = "Creating new synopsis for {0}";
        private const string CommitUrl = "git/commits";
        private const string CreateNewBranchUrl = "git/refs";
        private const string CreateTreeUrl = "git/trees";
        private const string EmailMarker = "<!-- ENTER YOUR EMAIL HERE -->";
        private const string GetHeadsUrl = "git/refs/heads";
        private const string GitHubRepo = "GitHubRepo";
        private const string GitHubToken = "GitHubToken";
        private const string MasterHead = "/master";
        private const string NameMarker = "<!-- ENTER YOUR NAME HERE -->";
        private const string RawTemplateUrl = "https://raw.githubusercontent.com/lbugnion/{0}/master/templates/synopsis-template.md";
        private const string RepoUrl = "https://github.com/lbugnion/{0}/blob/{1}/synopsis/{1}.md";
        private const string ShortDescriptionMarker = "<!-- ENTER A SHORT DESCRIPTION HERE -->";
        private const string TopicMarker = "<!-- TOPIC -->";
        private const string TwitterMarker = "<!-- ENTER YOUR TWITTER NAME HERE -->";
        private const string UpdateReferenceUrl = "git/refs/heads/{0}";
        private const string UploadBlobUrl = "git/blobs";

        // See http://www.levibotelho.com/development/commit-a-file-with-the-github-api/

        [FunctionName("AddSynopsis")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = "add-new")]
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation("In AddSynopsis");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var newTopic = JsonConvert.DeserializeObject<NewTopicInfo>(requestBody);

            if (string.IsNullOrEmpty(newTopic.SubmitterName)
                || string.IsNullOrEmpty(newTopic.SubmitterEmail)
                || string.IsNullOrEmpty(newTopic.Topic)
                || string.IsNullOrEmpty(newTopic.ShortDescription))
            {
                log.LogInformation("Incomplete submission");
                return new BadRequestObjectResult("Incomplete submission");
            }

            // Create new file in GitHub

            newTopic.SafeTopic = newTopic.Topic.MakeSafeFileName();
            log.LogInformation($"Safe topic: {newTopic.SafeTopic}");

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "WordsOfTheDayApp");

            var repoName = Environment.GetEnvironmentVariable(GitHubRepo);
            var token = Environment.GetEnvironmentVariable(GitHubToken);
            var url = string.Format(ApiBaseUrl, repoName, GetHeadsUrl);

            log.LogInformation($"repoName: {repoName}");
            log.LogInformation($"url: {url}");

            // Get heads

            log.LogInformation("Getting heads");

            var request = new HttpRequestMessage();
            request.RequestUri = new Uri(url);
            request.Method = HttpMethod.Get;

            var response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    var errorMessage = response.Content.ReadAsStringAsync();
                    log.LogInformation($"Error getting heads: {errorMessage}");
                    return new BadRequestObjectResult($"Error getting heads: {errorMessage}");
                }
                catch (Exception ex)
                {
                    log.LogInformation($"Unknown error getting heads: {ex.Message}");
                    return new BadRequestObjectResult($"Unknown error getting heads: {ex.Message}");
                }
            }

            var jsonResult = await response.Content.ReadAsStringAsync();
            var getHeadsResults = JsonConvert.DeserializeObject<IList<GetHeadResult>>(jsonResult);
            log.LogInformation($"Found {getHeadsResults.Count} heads");

            // Get master head

            log.LogInformation("Getting master head");

            var masterHead = getHeadsResults
                .Where(h => h.Ref.EndsWith(MasterHead))
                .FirstOrDefault();

            if (masterHead == null)
            {
                return new BadRequestObjectResult("Cannot locate master branch");
            }

            log.LogInformation($"Done getting master head {masterHead.Object.Sha}");

            // Create a new branch

            log.LogInformation("Creating new branch");

            var newBranchRequestBody = new NewBranchInfo
            {
                Sha = masterHead.Object.Sha,
                Ref = string.Format(NewBranchInfo.RefMask, newTopic.SafeTopic)
            };

            var jsonRequest = JsonConvert.SerializeObject(newBranchRequestBody);

            url = string.Format(ApiBaseUrl, repoName, CreateNewBranchUrl);
            request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Content = new StringContent(jsonRequest)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            response = await client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                var errorResultJson = await response.Content.ReadAsStringAsync();
                var errorResult = JsonConvert.DeserializeObject<ErrorResult>(errorResultJson);
                log.LogInformation($"Error when creating new branch: {newTopic.SafeTopic} / {errorResult.Message}");
                return new BadRequestObjectResult($"Error when creating new branch: {newTopic.SafeTopic} / {errorResult.Message}");
            }

            jsonResult = await response.Content.ReadAsStringAsync();
            var createNewBranchResult = JsonConvert.DeserializeObject<GetHeadResult>(jsonResult);
            log.LogInformation($"Done creating new branch {createNewBranchResult.Object.Sha}");

            // Grab master commit

            log.LogInformation("Grabbing master commit");

            request = new HttpRequestMessage();
            request.RequestUri = new Uri(masterHead.Object.Url);
            request.Method = HttpMethod.Get;

            response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    var errorMessage = response.Content.ReadAsStringAsync();
                    log.LogInformation($"Error getting commit: {errorMessage}");
                    return new BadRequestObjectResult($"Error getting commit: {errorMessage}");
                }
                catch (Exception ex)
                {
                    log.LogInformation($"Unknown error getting commit: {ex.Message}");
                    return new BadRequestObjectResult($"Unknown error getting commit: {ex.Message}");
                }
            }

            jsonResult = await response.Content.ReadAsStringAsync();
            var masterCommitResult = JsonConvert.DeserializeObject<CommitResult>(jsonResult);
            log.LogInformation($"Done grabbing master commit {masterCommitResult.Sha}");

            // Get file template from GitHub

            log.LogInformation("Getting file template from GitHub");
            var templateUrl = string.Format(RawTemplateUrl, repoName);
            var markdownTemplate = await client.GetStringAsync(templateUrl);

            markdownTemplate = markdownTemplate
                .Replace(TopicMarker, newTopic.Topic)
                .Replace(NameMarker, newTopic.SubmitterName)
                .Replace(EmailMarker, newTopic.SubmitterEmail)
                .Replace(ShortDescriptionMarker, newTopic.ShortDescription);

            if (!string.IsNullOrEmpty(newTopic.SubmitterTwitter))
            {
                if (!newTopic.SubmitterTwitter.StartsWith('@'))
                {
                    newTopic.SubmitterTwitter = $"@{newTopic.SubmitterTwitter}";
                }

                markdownTemplate = markdownTemplate.Replace(TwitterMarker, newTopic.SubmitterTwitter);
            }
            else
            {
                markdownTemplate = markdownTemplate.Replace(TwitterMarker, string.Empty);
            }

            log.LogInformation("Done getting file template from GitHub");

            // Post new file to GitHub blob

            log.LogInformation("Posting to GitHub blob");
            var uploadInfo = new UploadInfo
            {
                Content = markdownTemplate
            };

            jsonRequest = JsonConvert.SerializeObject(uploadInfo);

            url = string.Format(ApiBaseUrl, repoName, UploadBlobUrl);
            request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Content = new StringContent(jsonRequest)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                try
                {
                    var errorMessage = response.Content.ReadAsStringAsync();
                    log.LogInformation($"Error uploading blob: {errorMessage}");
                    return new BadRequestObjectResult($"Error uploading blob: {errorMessage}");
                }
                catch (Exception ex)
                {
                    log.LogInformation($"Unknown error uploading blob: {ex.Message}");
                    return new BadRequestObjectResult($"Unknown error uploading blob: {ex.Message}");
                }
            }

            jsonResult = await response.Content.ReadAsStringAsync();
            var uploadBlobResult = JsonConvert.DeserializeObject<ShaInfo>(jsonResult);
            log.LogInformation($"Done posting to GitHub blob {uploadBlobResult.Sha}");

            // Create the tree

            log.LogInformation("Creating the tree");
            var treeInfo = new CreateTreeInfo(newTopic.SafeTopic, uploadBlobResult.Sha)
            {
                BaseTree = masterCommitResult.Tree.Sha,
            };

            jsonRequest = JsonConvert.SerializeObject(treeInfo);

            url = string.Format(ApiBaseUrl, repoName, CreateTreeUrl);
            request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Content = new StringContent(jsonRequest)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                try
                {
                    var message = response.Content.ReadAsStringAsync();
                    log.LogInformation($"Error creating tree: {message}");
                    return new BadRequestObjectResult($"Error creating tree: {message}");
                }
                catch (Exception ex)
                {
                    log.LogInformation($"Unknown error creating tree: {ex.Message}");
                    return new BadRequestObjectResult($"Unknown error creating tree: {ex.Message}");
                }
            }

            jsonResult = await response.Content.ReadAsStringAsync();
            var createTreeResult = JsonConvert.DeserializeObject<ShaInfo>(jsonResult);
            log.LogInformation($"Done creating the tree {createTreeResult.Sha}");

            // Create the commit

            log.LogInformation("Creating the commit");
            var commitMessage = string.Format(CommitMessage, newTopic.SafeTopic);
            var commitInfo = new CommitInfo(commitMessage, masterCommitResult.Sha, createTreeResult.Sha);

            log.LogInformation($"commitMessage: {commitMessage}");

            jsonRequest = JsonConvert.SerializeObject(commitInfo);

            url = string.Format(ApiBaseUrl, repoName, CommitUrl);
            request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Content = new StringContent(jsonRequest)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                try
                {
                    var message = response.Content.ReadAsStringAsync();
                    log.LogInformation($"Error creating commit: {message}");
                    return new BadRequestObjectResult($"Error creating commit: {message}");
                }
                catch (Exception ex)
                {
                    log.LogInformation($"Unknown error creating commit: {ex.Message}");
                    return new BadRequestObjectResult($"Unknown error creating commit: {ex.Message}");
                }
            }

            jsonResult = await response.Content.ReadAsStringAsync();
            var createCommitResult = JsonConvert.DeserializeObject<ShaInfo>(jsonResult);
            log.LogInformation($"Done creating the commit {createCommitResult.Sha}");

            // Update reference

            log.LogInformation("Updating the reference");
            var updateReferenceInfo = new UpdateReferenceInfo(createCommitResult.Sha);

            jsonRequest = JsonConvert.SerializeObject(updateReferenceInfo);

            url = string.Format(ApiBaseUrl, repoName, string.Format(UpdateReferenceUrl, newTopic.SafeTopic));
            request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Patch,
                Content = new StringContent(jsonRequest)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    var message = response.Content.ReadAsStringAsync();
                    log.LogInformation($"Error updating reference: {message}");
                    return new BadRequestObjectResult($"Error updating reference: {message}");
                }
                catch (Exception ex)
                {
                    log.LogInformation($"Unknown error updating reference: {ex.Message}");
                    return new BadRequestObjectResult($"Unknown error updating reference: {ex.Message}");
                }
            }

            jsonResult = await response.Content.ReadAsStringAsync();
            var headResult = JsonConvert.DeserializeObject<GetHeadResult>(jsonResult);
            log.LogInformation("Done updating the reference");

            newTopic.Ref = headResult.Ref;
            newTopic.Url = string.Format(RepoUrl, repoName, newTopic.SafeTopic);
            jsonResult = JsonConvert.SerializeObject(newTopic);

            log.LogInformation($"newTopic.Ref: {newTopic.Ref}");
            log.LogInformation($"newTopic.Url: {newTopic.Url}");

            return new OkObjectResult(jsonResult);
        }
    }
}