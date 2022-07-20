using Azure.Messaging.ServiceBus;
using NetLah.Extensions.Configuration;

namespace EchoServiceApi.Verifiers
{
    public class MessageBusVerifierQueueName
    {
        public string? QueueName { get; set; }
    }

    public class MessageBusVerifier : BaseVerifier
    {
        public MessageBusVerifier(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public async Task<VerifyResult> VerifyAsync(string name, bool send, bool receive, string? queueName)
        {
            var connectionObj = GetConnection(name);

            IDisposable? scope = null;

            var serviceBusFqns = connectionObj.GetServiceBusFqns();
            ServiceBusClient client;

            if (serviceBusFqns == null)
            {
                if (string.IsNullOrEmpty(queueName))
                {
                    queueName = connectionObj.Get<MessageBusVerifierQueueName>().QueueName ?? throw new Exception("QueueName is required");
                }
                client = new ServiceBusClient(connectionString: connectionObj.Value);
            }
            else
            {
                if (string.IsNullOrEmpty(queueName))
                {
                    queueName = serviceBusFqns.QueueName ?? throw new Exception("QueueName is required");
                }
                var tokenCredential = await TokenFactory.GetTokenCredentialOrDefaultAsync(serviceBusFqns);
                client = new ServiceBusClient(serviceBusFqns.Fqns, tokenCredential);
                scope = LoggerBeginScopeDiagnostic();
            }

            Logger.LogInformation("MessageBusVerifier: name={query_name} send={query_send} receive={query_receive} queueName={query_queueName}",
                name, send, receive, queueName);

            ServiceBusSender? sender = null;
            ServiceBusReceiver? receiver = null;
            try
            {
                sender = client.CreateSender(queueName);
                receiver = client.CreateReceiver(queueName);
                if (receive)
                {
                    var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(1));
                    var isExist = message != null;
                    if (isExist)
                    {
                        await receiver.CompleteMessageAsync(message);
                    }
                    var detail1 = $"Received={isExist}; queueName={queueName}; messageId={message?.MessageId}; messageBody={message?.Body.ToString()}";
                    return VerifyResult.Successed("MessageBus", connectionObj, detail1);
                }
                else if (send)
                {
                    await sender.SendMessageAsync(new ServiceBusMessage($"{queueName}-{DateTimeOffset.Now}"));

                    var detail1 = $"Status=Sent; queueName={queueName}; fqn={sender.FullyQualifiedNamespace};";
                    return VerifyResult.Successed("MessageBus", connectionObj, detail1);
                }

                var detail = $"queueName={queueName}; fqns={sender.FullyQualifiedNamespace};";
                return VerifyResult.Successed("MessageBus", connectionObj, detail);
            }
            finally
            {
                if (receiver != null)
                    await receiver.DisposeAsync();

                if (sender != null)
                    await sender.DisposeAsync();

                await client.DisposeAsync();

                scope?.Dispose();
            }
        }
    }
}
