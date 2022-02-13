using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;




namespace My.HttpToCosmos
{
    public static class HttpToCosmos
    {
        // Create a single, static HttpClient
        private static HttpClient httpClient = new HttpClient();

        [FunctionName("HttpToCosmos")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "my-database", 
                collectionName: "my-container", 
                ConnectionStringSetting = "CosmosDbConnectionString")]IAsyncCollector<dynamic> documentsOut,
            ILogger log)
        {

            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try	
            {
                HttpResponseMessage response = await httpClient.GetAsync("https://myuniquehttpfunction.azurewebsites.net/api/httpexample?name=Another");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                log.LogInformation(responseBody);
            }
            catch(HttpRequestException e)
            {
                log.LogInformation("\nException Caught!");	
                log.LogInformation("Message :{0} ",e.Message);
            }


            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            if (!string.IsNullOrEmpty(name))
            {
                // Add a JSON document to the output container.
                await documentsOut.AddAsync(new
                {
                    // create a random ID
                    id = System.Guid.NewGuid().ToString(),
                    name = name
                });
            }

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
