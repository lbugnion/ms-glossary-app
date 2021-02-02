using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using MsGlossaryApp.DataModel;
using MsGlossaryApp.DataModel.Pass;
using System;
using System.Threading.Tasks;

namespace MsGlossaryApp.Model.Pass
{
    public class PassHandler
    {
        private string _connectionString;

        public PassHandler(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<(bool valid, bool first)> Verify(
            string userEmail, 
            string fileName, 
            string hash,
            ILogger log = null)
        {
            log?.LogInformation("-> Verify");

            var table = await GetTable(log);
            var existingPass = await RetrieveExisting(userEmail, fileName, table);

            if (existingPass == null)
            {
                log.LogWarning("No row found");
                return (false, false);
            }

            var isValid = existingPass.Hash == hash;
            var first = isValid ? existingPass.FirstLogin : false;

            log.LogDebug($"Valid: {isValid}");

            return (isValid, first);
        }

        private async Task<PassEntity> RetrieveExisting(
            string userEmail,
            string fileName,
            CloudTable table)
        {
            // Try to retrieve an existing entity
            var retrieveOperation = TableOperation.Retrieve<PassEntity>(
                userEmail.ToLower(),
                fileName.ToLower());

            var retrieveResult = await table.ExecuteAsync(retrieveOperation);
            var existingPass = retrieveResult.Result as PassEntity;

            return existingPass;
        }

        internal async Task<bool> Initialize(
            string userEmail, 
            string fileName,
            ILogger log = null)
        {
            log?.LogInformation("-> Initialize");

            var table = await GetTable(log);
            var existingPass = await RetrieveExisting(userEmail, fileName, table);

            if (existingPass != null)
            {
                return false;
            }

            var initialHash = "1234"; // TODO Create real hash

            var passEntity = new PassEntity
            {
                PartitionKey = userEmail.ToLower(),
                RowKey = fileName.ToLower(),
                Hash = initialHash,
                FirstLogin = true
            };

            var insertOperation = TableOperation.Insert(passEntity);
            await table.ExecuteAsync(insertOperation);

            return true;
        }

        public async Task<bool> Change(
            string userEmail, 
            string fileName, 
            string oldHash, 
            string newHash,
            ILogger log = null)
        {
            log?.LogInformation("-> Change");

            var table = await GetTable(log);
            var existingPass = await RetrieveExisting(userEmail, fileName, table);

            if (existingPass == null)
            {
                return false;
            }

            if (existingPass.Hash != oldHash)
            {
                return false;
            }

            existingPass.Hash = newHash;
            existingPass.FirstLogin = false;

            var mergeOperation = TableOperation.Merge(existingPass);
            await table.ExecuteAsync(mergeOperation);
            return true;
        }

        private async Task<CloudTable> GetTable(ILogger log = null)
        {
            log?.LogInformation("-> GetTable");

            var storageAccount = CloudStorageAccount.Parse(_connectionString);

            var tableClient = storageAccount.CreateCloudTableClient(
                new TableClientConfiguration());

            var tableName = Environment.GetEnvironmentVariable(
                Constants.HashTableVariableName);

            CloudTable table = tableClient.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync();

            log?.LogInformation("GetTable ->");
            return table;
        }
    }
}
