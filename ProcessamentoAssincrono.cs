using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace ANP.Ecommerce
{
    public static class ProcessamentoAssincrono
    {
        [FunctionName("Workflow")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var parallelTasks = new List<Task<string>>();

            foreach (var itemFile in Directory.GetFiles($@"C:\Projetos\Azure na Pratica\Azure Functions\DotNet-DurableParalelo\Files\"))
            {
                log.LogInformation($"Processando arquivo { itemFile }");

                var task = context.CallActivityAsync<string>("ProcessamentoArquivo", itemFile);
                parallelTasks.Add(task);
            }

            await Task.WhenAll(parallelTasks);

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            log.LogInformation($"Finalizando Processamento");
            //return parallelTasks.Select(x => x.Result).ToList();
        }

        [FunctionName("ProcessamentoArquivo")]
        public static string ProcessamentoArquivo([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            string fileName = context.GetInput<string>();

            log.LogInformation($"Acessando arquivo {fileName}.");
            using (var sr = new StreamReader(fileName))
            {
                var resultadoArquivo = sr.ReadToEnd();
                return resultadoArquivo;
            }
        }

        [FunctionName("ProcessamentoAssincrono_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Workflow", null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}