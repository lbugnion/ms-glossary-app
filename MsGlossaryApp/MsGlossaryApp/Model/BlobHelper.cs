using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System;

namespace MsGlossaryApp.Model
{
    public class BlobHelper
    {
        public CloudBlobClient _client;

        public ILogger _logger;

        public BlobHelper(CloudBlobClient client, ILogger logger = null)
        {
            _logger = logger;
            _client = client ?? throw new ArgumentNullException("client");
        }

        public CloudBlobContainer GetContainer(string variableName)
        {
            var containerName = Environment.GetEnvironmentVariable(variableName);
            _logger?.LogInformation($"containerName: {variableName} : {containerName}");
            var container = _client.GetContainerReference(containerName);
            _logger?.LogInformation($"container: {variableName} : {container.Uri}");
            return container;
        }
    }
}