using System.Collections.Concurrent;
using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OrderProcessor
{
    public class OrderProcessor
    {
        private readonly ILogger<OrderProcessor> _logger;
        private readonly string KeyVaultUri = "https://finaltask.vault.azure.net/";

        private readonly string DatabaseName = "orderconfirmedorderDb";
        private readonly string ContainerName = "orderconfirmed";
        public OrderProcessor(ILogger<OrderProcessor> logger)
        {
            _logger = logger;
        }

        [Function("OrderProcessor")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("OrderProcessor function processing a request.");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                if (string.IsNullOrEmpty(requestBody))
                {
                    return new BadRequestObjectResult("Invalid order request");
                }
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                data.id = $"{Guid.NewGuid().ToString()}_{data.OrderID}";
                data.timestamp = DateTime.UtcNow;
                string response = await AddItemToCosmosDB(data);
                if (response != null && response.Equals("success"))
                {
                    return new OkObjectResult($"Order placed successfully");
                }
                return new ObjectResult($"Order failed to save into Database");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error : {ex.ToString()}");
                return new BadRequestObjectResult($"Exception in OrderProcessor");
            }

        }

        private async Task<string> AddItemToCosmosDB(dynamic data)
        {
            try
            {
                var secretClient = new SecretClient(new Uri(KeyVaultUri), new DefaultAzureCredential());

                KeyVaultSecret CosmosDBUri = await secretClient.GetSecretAsync("CosmosDbUrlString");
                KeyVaultSecret PrimaryKey = await secretClient.GetSecretAsync("CosmosDbPrimaryKeyString");

                using (CosmosClient client = new CosmosClient(CosmosDBUri.Value, PrimaryKey.Value))
                {
                    var database = client.GetDatabase(DatabaseName);
                    var container = database.GetContainer(ContainerName);
                    await container.CreateItemAsync(data, new PartitionKey((string)data.id));
                    _logger.LogInformation("Data successfully inserted in CosmosDB");
                }
                return "success";
            }
            catch (Exception ex)
            {
                _logger.LogError($"AddItemToCosmosDB : {ex.ToString()}");
                return "Error while inserting into cosmosDB";
            }
        }
    }
}
