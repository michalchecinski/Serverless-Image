using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerlessImage.Helpers
{
    using Microsoft.Azure.ServiceBus;

    public class ServiceBusQueueHelpers
    {
        public static async Task SendMessageAsync(string queue, string message, string serviceBusConnectionString)
        {
            Message queueMessage = new Message(Encoding.UTF8.GetBytes(message));
            QueueClient client = new QueueClient(serviceBusConnectionString, queue);
            await client.SendAsync(queueMessage);
            await client.CloseAsync();
        }
    }
}
