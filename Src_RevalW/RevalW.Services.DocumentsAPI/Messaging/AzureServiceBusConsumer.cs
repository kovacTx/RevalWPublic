using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using RevalW.Services.DocumentsAPI.Services;
using RevalW.Services.DocumentsAPI.Utils;
using RevalW.Shared.Model;
using System.Text;

namespace RevalW.Services.DocumentsAPI.Messaging
{
    public class AzureServiceBusConsumer(CommonData commonData, DocumentsService documentsService) : IAzureServiceBusConsumer
    {
        private ServiceBusClient? _serviceBusClient;
        private ServiceBusClient ServiceBusClient => _serviceBusClient ??= new ServiceBusClient(commonData.DefaultQueueConnection);

        private ServiceBusProcessor? _generateDocumentQueueProcessor;
        private ServiceBusProcessor GenerateDocumentQueueProcessor => _generateDocumentQueueProcessor ??= ServiceBusClient.CreateProcessor(commonData.GenerateDocumentQueueName);

        public async Task Start()
        {
            GenerateDocumentQueueProcessor.ProcessMessageAsync += GenerateDocumentQueueProcessor_ProcessMessageAsync;
            GenerateDocumentQueueProcessor.ProcessErrorAsync += GenerateDocumentQueueProcessor_ProcessErrorAsync;
            await GenerateDocumentQueueProcessor.StartProcessingAsync();
        }
        public async Task Stop()
        {
            await GenerateDocumentQueueProcessor.StopProcessingAsync();
            await GenerateDocumentQueueProcessor.DisposeAsync();
        }

        private async Task GenerateDocumentQueueProcessor_ProcessMessageAsync(ProcessMessageEventArgs arg)
        {
            var message = arg.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            var content = JsonConvert.DeserializeObject<DocumentGenerateRequest>(body);
            try
            {
                await documentsService.Create(content);
                await arg.CompleteMessageAsync(arg.Message);
            }
            catch
            {
                await arg.CompleteMessageAsync(arg.Message);
                throw;
            }
        }
        private Task GenerateDocumentQueueProcessor_ProcessErrorAsync(ProcessErrorEventArgs arg)
        {
            Console.WriteLine(arg.Exception.ToString());
            return Task.CompletedTask;
        }
    }
}
