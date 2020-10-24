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
            _logger?.LogInformationEx($"containerName: {variableName} : {containerName}", LogVerbosity.Verbose);
            var container = _client.GetContainerReference(containerName);
            _logger?.LogInformationEx($"container: {variableName} : {container.Uri}", LogVerbosity.Verbose);
            return container;
        }
    }
}