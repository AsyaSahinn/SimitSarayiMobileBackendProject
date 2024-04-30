using EventBus.Base;
using EventBus.Base.Events;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.AzureServiceBus
{
    public class EventBusServiceBus : BaseEventBus
    {
        private ITopicClient topicClient;
        private ManagementClient managementClient;
        private ILogger logger;

        public EventBusServiceBus(EventBusConfig config, IServiceProvider serviceProvider) : base(config, serviceProvider)
        {
            logger = serviceProvider.GetService(typeof(ILogger<EventBusServiceBus>)) as ILogger<EventBusServiceBus>;
            managementClient = new ManagementClient(config.EventBusConnectionString);
            topicClient = CreateTopicClient();
        }

        private ITopicClient CreateTopicClient()
        {
            if (topicClient == null || topicClient.IsClosedOrClosing)
            {
                topicClient = new TopicClient(EventBusConfig.EventBusConnectionString, EventBusConfig.DefaultTopicName, RetryPolicy.Default);
            }
            //ensure that topic already exists
            if (!managementClient.TopicExistsAsync(EventBusConfig.DefaultTopicName).GetAwaiter().GetResult())
            {
                managementClient.CreateTopicAsync(EventBusConfig.DefaultTopicName).GetAwaiter().GetResult();
            }
            return topicClient;
        }

        public override void Publish(IntegrationEvent @event)
        {

            var eventName = @event.GetType().Name;  //exapmle OrderCreatedIntegrationEvent

            eventName = ProcessEventName(eventName); //example OrderCreated

            var eventStr = JsonConvert.SerializeObject(@event); // convert event to string
            var bodyArr = Encoding.UTF8.GetBytes(eventStr); // convert string to array


            var message = new Message()
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = bodyArr,
                Label = ""
            };

            //Edited message then send to service bus
            topicClient.SendAsync(message).GetAwaiter().GetResult();
        }

        public override void Subscribe<T, TH>()
        {
            var eventName = typeof(T).Name;
            eventName = ProcessEventName(eventName);

            if (!SubsManager.HasSubscriptionsForEvent(eventName))
            {
                var subscriptionClient = CreateSubscriptionClientIfNotExist(eventName);

                //Forward incoming messages
                RegisterSubscriptionClientMessageHandler(subscriptionClient);
            }

            logger.LogInformation("Subscribing to event {EventName}", eventName, typeof(TH).Name);

            SubsManager.AddSubscription<T, TH>();
        }

        public override void UnSubscribe<T, TH>()
        {
            var eventName = typeof(T).Name;

            try
            {
                //Subscription  will be there but we dont subscribe 

                var subscribtionClient = CreateSubscriptionClient(eventName);

                subscribtionClient
                    .RemoveRuleAsync(eventName)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (MessageNotFoundException) 
            {
                logger.LogWarning("The messaging entity {eventName} could be not found.", eventName);
            }
            logger.LogInformation("Unsubcribing from event {eventName}", eventName);

            SubsManager.RemoveSubscription<T, TH>();
        }

        //Sana gelen herhangi bir mesaj var ise, bu mesaj geldiğinde bana haber ver
        private void RegisterSubscriptionClientMessageHandler(ISubscriptionClient subscriptionClient)
        {
            subscriptionClient.RegisterMessageHandler(
                async (message, token) =>
                {
                    var eventName = $"{message.Label}";
                    var messageData = Encoding.UTF8.GetString(message.Body);

                    //Set message status Completed that it is not received again.
                    if (await ProcessEvent(ProcessEventName(eventName), messageData))
                    {
                        await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
                    }
                },
                new MessageHandlerOptions(ExceptionReceivedHandler) { MaxConcurrentCalls = 10, AutoComplete = false }
                );
        }
        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            var ex = exceptionReceivedEventArgs.Exception;
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

            logger.LogError(ex, "ERROR Handling message : {ExceptionMessage} - Context : {@ExceptionContext}", ex.Message, context);

            return Task.CompletedTask;
        }
        private ISubscriptionClient CreateSubscriptionClientIfNotExist(string eventName)
        {
            var subClient = CreateSubscriptionClient(eventName);

            var exists = managementClient.SubscriptionExistsAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult();

            //If there is no subscription then create new subscription and remove subscription's default rule.
            if (!exists)
            {
                managementClient.CreateSubscriptionAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult();
                RemoveDefaultRole(subClient);
            }
            CreateRuleIfNotExists(ProcessEventName(eventName), subClient);
            return subClient;
        }

        private void CreateRuleIfNotExists(string eventName, ISubscriptionClient subscriptionClient)
        {
            bool ruleExists;

            try
            {
                var rule = managementClient.GetRuleAsync(EventBusConfig.DefaultTopicName, eventName, eventName).GetAwaiter().GetResult();
                ruleExists = rule != null;
            }
            catch (MessagingEntityNotFoundException)
            {
                // Azure Management Client doesnt have RuleExists method
                ruleExists = false;
            }

            if (!ruleExists)
            {
                subscriptionClient.AddRuleAsync(new RuleDescription
                {
                    Filter = new CorrelationFilter { Label = eventName },
                    Name = eventName,
                }).GetAwaiter().GetResult();
            }
        }

        private void RemoveDefaultRole(SubscriptionClient subscriptionClient)
        {
            try
            {
                subscriptionClient
                    .RemoveRuleAsync(RuleDescription.DefaultRuleName)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
                logger.LogWarning("The message entity {DefaultRuleName} could not be found", RuleDescription.DefaultRuleName);
            }
        }

        private SubscriptionClient CreateSubscriptionClient(string eventName)
        {
            return new SubscriptionClient(EventBusConfig.EventBusConnectionString, EventBusConfig.DefaultTopicName, GetSubName(eventName));
        }

        public override void Dispose()
        {
            //garbage collecter 
            base.Dispose();

            topicClient.CloseAsync().GetAwaiter().GetResult();
            managementClient.CloseAsync().GetAwaiter().GetResult();
            topicClient = null;
            managementClient = null;
          
        }


    }
}
