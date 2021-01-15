using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MsGlossaryApp.DataModel;
using MsGlossaryApp.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MsGlossaryApp.TestDownloadBlobs
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(
                    Constants.AzureWebJobsStorageVariableName));

            var blobClient = account.CreateCloudBlobClient();
            var blobHelper = new BlobHelper(blobClient, null);

            var containers = new List<CloudBlobContainer>()
            {
                blobHelper.GetContainerFromName(
                    "output"),
                blobHelper.GetContainerFromName(
                    "output-staging"),
                blobHelper.GetContainerFromName(
                    "output-testing"),
                blobHelper.GetContainerFromName(
                    "settings"),
                blobHelper.GetContainerFromName(
                    "settings-staging"),
                blobHelper.GetContainerFromName(
                    "settings-testing"),
                blobHelper.GetContainerFromName(
                    "captions"),
                blobHelper.GetContainerFromName(
                    "terms"),
                blobHelper.GetContainerFromName(
                    "terms-testing")
            };

            Console.WriteLine("Ready, press any key except N to start");

            while (Console.ReadLine() != "N")
            {
                foreach (var container in containers)
                {
                    BlobContinuationToken continuationToken = null;

                    var outputFolder = new DirectoryInfo(
                        Path.Combine(@"C:\Users\lbugnion\Desktop\blobs", container.Name));

                    if (!outputFolder.Exists)
                    {
                        outputFolder.Create();
                    }

                    do
                    {
                        var response = await container.ListBlobsSegmentedAsync(continuationToken);
                        continuationToken = response.ContinuationToken;

                        foreach (CloudBlockBlob blob in response.Results)
                        {
                            var content = await blob.DownloadTextAsync();
                            string fileName;
                            DirectoryInfo blobFolder = outputFolder;

                            var nameParts = blob.Name.Split(new char[]
                            {
                                '_'
                            });

                            var root = outputFolder.FullName;

                            for (var index = 0; index < nameParts.Length - 1; index++)
                            {
                                blobFolder = new DirectoryInfo(
                                    Path.Combine(
                                        blobFolder.FullName,
                                        nameParts[index]));

                                if (!blobFolder.Exists)
                                {
                                    blobFolder.Create();
                                }
                            }

                            fileName = nameParts[nameParts.Length - 1];

                            var file = new FileInfo(
                                Path.Combine(
                                    blobFolder.FullName,
                                    fileName));

                            using (var writer = new StreamWriter(file.FullName))
                            {
                                writer.Write(content);
                            }

                            Console.WriteLine(file.FullName);
                        }
                    }
                    while (continuationToken != null);
                }

                Console.WriteLine("Press N to finish, any key to download again");
            }

        }
    }
}