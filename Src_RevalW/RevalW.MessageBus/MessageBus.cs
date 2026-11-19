using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using System.Text;

namespace RevalW.MessageBus;

public class MessageBus : IMessageBus
{
    public async Task PublishMessage(object message, string? topic_queue_name, string connectionStrinmg)
    {
        if (topic_queue_name is null) return;
        await using var client = new ServiceBusClient(connectionStrinmg);
        var sender = client.CreateSender(topic_queue_name);
        var jsonMessage = JsonConvert.SerializeObject(message);
        var finalMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(jsonMessage))
        {
            CorrelationId = Guid.NewGuid().ToString(),
        };

        await sender.SendMessageAsync(finalMessage);
        await client.DisposeAsync();
    }
}