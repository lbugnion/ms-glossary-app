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
    public static class SettingsFilesSaver
    {
        private const string SideBarBoldTemplate = "- [**{0}**](/topic/{1}/{2})";
        private const string SideBarTemplate = "- [{0}](/topic/{1}/{2}_{3})";

        public static async Task SaveSideBar(string languageCode, ILogger log)
        {
            log?.LogInformation("Saving SideBar");

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));
            var client = account.CreateCloudBlobClient();
            var helper = new BlobHelper(client, log);

            var settingsContainer = helper.GetContainer(Constants.SettingsContainerVariableName);
            var keywordsBlob = settingsContainer.GetBlockBlobReference(
                string.Format(Constants.KeywordsBlob, languageCode));

            if (!await keywordsBlob.ExistsAsync())
            {
                log.LogError($"Keywords blob not found: {keywordsBlob.Uri}");
                return;
            }

            var json = await keywordsBlob.DownloadTextAsync();
            var keywordsDictionary = JsonConvert.DeserializeObject<Dictionary<char, List<KeywordPair>>>(json);

            var sideBarMarkdownBlob = settingsContainer.GetBlockBlobReference(
                string.Format(Constants.SideBarMarkdownBlob, languageCode));

            var md = new StringBuilder();
            foreach (var pair in keywordsDictionary.OrderBy(p => p.Key))
            {
                md.AppendLine($"# {pair.Key}");
                md.AppendLine();

                log?.LogInformation($"Side bar: {pair.Key}");

                foreach (var k in pair.Value.Where(v => !v.MustDisambiguate).OrderBy(v => v.Keyword))
                {
                    log?.LogInformation($"Side bar: {languageCode} {k.Keyword} | {k.Topic}");

                    if (k.Topic == k.Subtopic)
                    {
                        md.AppendLine(string.Format(SideBarBoldTemplate, k.Keyword, languageCode, k.Topic));
                    }
                    else
                    {
                        md.AppendLine(string.Format(SideBarTemplate, k.Keyword, languageCode, k.Topic, k.Subtopic));
                    }
                }

                md.AppendLine();
            }

            await sideBarMarkdownBlob.UploadTextAsync(md.ToString());
        }

        public static async Task SaveTopics(string languageCode, IList<TopicInformation> topics, ILogger log)
        {
            log?.LogInformation("Saving topics");

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));
            var client = account.CreateCloudBlobClient();
            var helper = new BlobHelper(client, log);

            var settingsContainer = helper.GetContainer(Constants.SettingsContainerVariableName);
            var topicsJsonBlob = settingsContainer.GetBlockBlobReference(
                string.Format(Constants.TopicsBlob, languageCode));

            var list = topics.Select(t => t.TopicName);

            var json = JsonConvert.SerializeObject(list);
            await topicsJsonBlob.UploadTextAsync(json);

            log?.LogInformation($"Saved topics {json} in {topicsJsonBlob.Uri}");
        }

        public static async Task SaveLanguages(IList<LanguageInfo> languages, ILogger log)
        {
            log?.LogInformation("Saving topics");

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));
            var client = account.CreateCloudBlobClient();
            var helper = new BlobHelper(client, log);

            var settingsContainer = helper.GetContainer(Constants.SettingsContainerVariableName);
            var languagesBlob = settingsContainer.GetBlockBlobReference(Constants.LanguagesBlob);

            var builder = new StringBuilder();
            builder.Append(Constants.SiteLanguages);

            foreach (var language in languages)
            {
                builder.Append(language.ToString());
                builder.Append(", ");
            }

            builder.Remove(builder.Length - 2, 1);

            await languagesBlob.UploadTextAsync(builder.ToString());

            log?.LogInformation($"Saved languages in {languagesBlob.Uri}");
        }
    }
}