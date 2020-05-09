using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordsOfTheDayApp.Model
{
    public static class TopicsListSaver
    {
        private const string SideBarBoldTemplate = "- [**{0}**](/topic/{1})";
        private const string SideBarTemplate = "- [{0}](/topic/{1}/{2})";

        public static async Task SaveTopics(IList<string> topics, ILogger log)
        {
            log?.LogInformation("Saving topics");

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));
            var client = account.CreateCloudBlobClient();
            var helper = new BlobHelper(client, log);

            var settingsContainer = helper.GetContainer(Constants.SettingsContainerVariableName);
            var topicsJsonBlob = settingsContainer.GetBlockBlobReference(Constants.TopicsBlob);
            var json = JsonConvert.SerializeObject(topics);
            await topicsJsonBlob.UploadTextAsync(json);

            log?.LogInformation($"Saved topics {json} in {topicsJsonBlob.Uri}");
        }

        public static async Task SaveSideBar(ILogger log)
        {
            log?.LogInformation("Saving SideBar");

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));
            var client = account.CreateCloudBlobClient();
            var helper = new BlobHelper(client, log);

            var settingsContainer = helper.GetContainer(Constants.SettingsContainerVariableName);
            var keywordsBlob = settingsContainer.GetBlockBlobReference(Constants.KeywordsBlob);

            if (!await keywordsBlob.ExistsAsync())
            {
                log.LogError($"Keywords blob not found: {keywordsBlob.Uri}");
                return;
            }

            var json = await keywordsBlob.DownloadTextAsync();
            var keywordsDictionary = JsonConvert.DeserializeObject<Dictionary<char, List<KeywordPair>>>(json);

            var sideBarMarkdownBlob = settingsContainer.GetBlockBlobReference(Constants.SideBarMarkdownBlob);

            var md = new StringBuilder();
            foreach (var pair in keywordsDictionary.OrderBy(p => p.Key))
            {
                md.AppendLine($"#### {pair.Key}");
                md.AppendLine();

                log?.LogInformation($"Side bar: {pair.Key}");

                foreach (var k in pair.Value.OrderBy(v => v.Keyword))
                {
                    log?.LogInformation($"Side bar: {k.Keyword} | {k.Topic}");

                    if (k.Topic == k.Subtopic)
                    {
                        md.AppendLine(string.Format(SideBarBoldTemplate, k.Keyword, k.Topic));
                    }
                    else
                    {
                        md.AppendLine(string.Format(SideBarTemplate, k.Keyword, k.Topic, k.Subtopic));
                    }
                }

                md.AppendLine();
            }

            await sideBarMarkdownBlob.UploadTextAsync(md.ToString());
        }
    }
}