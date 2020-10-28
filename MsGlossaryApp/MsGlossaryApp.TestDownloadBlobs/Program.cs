using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MsGlossaryApp.Model;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MsGlossaryApp.TestDownloadBlobs
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(
                    Constants.AzureWebJobsStorageVariableName));

            var blobClient = account.CreateCloudBlobClient();
            var blobHelper = new BlobHelper(blobClient, null);
            var outputContainer = blobHelper.GetContainer(
                Constants.OutputContainerVariableName);

            Console.WriteLine("Ready, press any key except N to start");

            while (Console.ReadLine() != "N")
            {
                BlobContinuationToken continuationToken = null;

                var outputFolder = new DirectoryInfo(@"C:\Users\lbugnion\Desktop\blobs");

                do
                {
                    var response = await outputContainer.ListBlobsSegmentedAsync(continuationToken);
                    continuationToken = response.ContinuationToken;

                    foreach (CloudBlockBlob blob in response.Results)
                    {
                        var content = await blob.DownloadTextAsync();
                        string fileName;
                        DirectoryInfo blobFolder;

                        if (blob.Name.ToLower() == "toc.yml")
                        {
                            fileName = blob.Name;
                            blobFolder = outputFolder;
                        }
                        else
                        {
                            var nameParts = blob.Name.Split(new char[]
                            {
                            '_'
                            });

                            blobFolder = new DirectoryInfo(
                                Path.Combine(
                                    outputFolder.FullName,
                                    nameParts[0]));

                            if (!blobFolder.Exists)
                            {
                                blobFolder.Create();
                            }

                            fileName = nameParts[1];
                        }

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

                Console.WriteLine("Press N to finish, any key to download again");
            }


        }
    }
}
