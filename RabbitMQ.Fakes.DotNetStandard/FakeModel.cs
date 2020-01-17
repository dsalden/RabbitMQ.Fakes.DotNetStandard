using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using RabbitMQ.Fakes.models;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Queue = RabbitMQ.Fakes.models.Queue;

namespace RabbitMQ.Fakes
{
    public class FakeModel : IModel
    {
        private readonly RabbitServer _server;
        private readonly ConcurrentBag<Exchange> _localExchanges = new ConcurrentBag<Exchange>();
        private readonly ConcurrentBag<Queue> _localQueues = new ConcurrentBag<Queue>();

        public FakeModel(RabbitServer server)
        {
            _server = server;
        }

        public IEnumerable<RabbitMessage> GetMessagesPublishedToExchange(string exchange)
        {
            Exchange exchangeInstance;
            _server.Exchanges.TryGetValue(exchange, out exchangeInstance);

            if (exchangeInstance == null)
                return new List<RabbitMessage>();

            return exchangeInstance.Messages;
        }

        public IEnumerable<RabbitMessage> GetMessagesOnQueue(string queueName)
        {
            Queue queueInstance;
            _server.Queues.TryGetValue(queueName, out queueInstance);

            if (queueInstance == null)
                return new List<RabbitMessage>();

            return queueInstance.Messages;
        }

        public bool ApplyPrefetchToAllChannels { get; private set; }
        public ushort PrefetchCount { get; private set; }
        public uint PrefetchSize { get; private set; }
        public bool IsChannelFlowActive { get; private set; }

        public void Dispose()
        {
        }

        public IBasicPublishBatch CreateBasicPublishBatch()
        {
            throw new NotImplementedException();
        }

        public IBasicProperties CreateBasicProperties()
        {
            return new BasicProperties();
        }

        public void ExchangeBindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        {
            throw new NotImplementedException();
        }

        public void ChannelFlow(bool active)
        {
            IsChannelFlowActive = active;
        }

        public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments)
        {
            var exchangeInstance = new Exchange
            {
                Name = exchange,
                Type = type,
                IsDurable = durable,
                IsAutoDelete = autoDelete,
                Arguments = arguments as IDictionary
            };
            Func<string, Exchange, Exchange> updateFunction = (name, existing) => existing;
            _server.Exchanges.AddOrUpdate(exchange, exchangeInstance, updateFunction);
            _localExchanges.Add(exchangeInstance);
        }

        public void ExchangeDeclare(string exchange, string type, bool durable)
        {
            ExchangeDeclare(exchange, type, durable, autoDelete: false, arguments: null);
        }

        public void ExchangeDeclare(string exchange, string type)
        {
            ExchangeDeclare(exchange, type, durable: false, autoDelete: false, arguments: null);
        }

        public void ExchangeDeclarePassive(string exchange)
        {
            ExchangeDeclare(exchange, type: null, durable: false, autoDelete: false, arguments: null);
        }

        public void ExchangeDeclareNoWait(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments)
        {
            ExchangeDeclare(exchange, type, durable, autoDelete: false, arguments: arguments);
        }

        public void ExchangeDelete(string exchange, bool ifUnused)
        {
            _server.Exchanges.TryRemove(exchange, out _);
        }

        public void ExchangeDelete(string exchange)
        {
            ExchangeDelete(exchange, ifUnused: false);
        }

        public void ExchangeDeleteNoWait(string exchange, bool ifUnused)
        {
            ExchangeDelete(exchange, ifUnused: false);
        }

        public void ExchangeUnbindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        {
            throw new NotImplementedException();
        }

        public void QueueBindNoWait(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        {
            throw new NotImplementedException();
        }

        public void ExchangeBind(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        {
            _server.Exchanges.TryGetValue(source, out var exchange);
            _server.Queues.TryGetValue(destination, out var queue);

            var binding = new ExchangeQueueBinding { Exchange = exchange, Queue = queue, RoutingKey = routingKey };
            exchange?.Bindings.AddOrUpdate(binding.Key, binding, (k, v) => binding);
            queue?.Bindings.AddOrUpdate(binding.Key, binding, (k, v) => binding);
        }

        public void ExchangeBind(string destination, string source, string routingKey)
        {
            ExchangeBind(destination: destination, source: source, routingKey: routingKey, arguments: null);
        }

        public void ExchangeUnbind(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        {
            _server.Exchanges.TryGetValue(source, out var exchange);
            _server.Queues.TryGetValue(destination, out var queue);

            var binding = new ExchangeQueueBinding { Exchange = exchange, Queue = queue, RoutingKey = routingKey };
            exchange?.Bindings.TryRemove(binding.Key, out _);
            queue?.Bindings.TryRemove(binding.Key, out _);
        }

        public void ExchangeUnbind(string destination, string source, string routingKey)
        {
            ExchangeUnbind(destination: destination, source: source, routingKey: routingKey, arguments: null);
        }

        public QueueDeclareOk QueueDeclare()
        {
            var name = Guid.NewGuid().ToString();
            return QueueDeclare(name, durable: false, exclusive: false, autoDelete: false, arguments: null);
        }

        public QueueDeclareOk QueueDeclarePassive(string queue)
        {
            return QueueDeclare(queue, durable: false, exclusive: false, autoDelete: false, arguments: null);
        }

        public uint MessageCount(string queue)
        {
            throw new NotImplementedException();
        }

        public uint ConsumerCount(string queue)
        {
            throw new NotImplementedException();
        }

        public void QueueBind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        {
            ExchangeBind(queue, exchange, routingKey, arguments);
        }

        public QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        {
            var queueInstance = new Queue
            {
                Name = queue,
                IsDurable = durable,
                IsExclusive = exclusive,
                IsAutoDelete = autoDelete,
                Arguments = arguments
            };

            Func<string, Queue, Queue> updateFunction = (name, existing) => existing;
            _server.Queues.AddOrUpdate(queue, queueInstance, updateFunction);
            _localQueues.Add(queueInstance);

            return new QueueDeclareOk(queue, 0, 0);
        }

        public void QueueDeclareNoWait(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        {
            QueueDeclare(queue, durable, exclusive, autoDelete, arguments);
        }

        public void QueueBind(string queue, string exchange, string routingKey)
        {
            ExchangeBind(queue, exchange, routingKey);
        }

        public void QueueUnbind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        {
            ExchangeUnbind(queue, exchange, routingKey);
        }

        public uint QueuePurge(string queue)
        {
            _server.Queues.TryGetValue(queue, out var instance);

            if (instance == null)
                return 0u;

            while (!instance.Messages.IsEmpty)
            {
                instance.Messages.TryDequeue(out _);
            }

            return 1u;
        }

        public uint QueueDelete(string queue, bool ifUnused, bool ifEmpty)
        {
            _server.Queues.TryRemove(queue, out var instance);

            return instance != null ? 1u : 0u;
        }

        public void QueueDeleteNoWait(string queue, bool ifUnused, bool ifEmpty)
        {
            QueueDelete(queue, ifUnused: false, ifEmpty: false);
        }

        public uint QueueDelete(string queue)
        {
            return QueueDelete(queue, ifUnused: false, ifEmpty: false);
        }

        public void ConfirmSelect()
        {
            throw new NotImplementedException();
        }

        public bool WaitForConfirms()
        {
            throw new NotImplementedException();
        }

        public bool WaitForConfirms(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public bool WaitForConfirms(TimeSpan timeout, out bool timedOut)
        {
            throw new NotImplementedException();
        }

        public void WaitForConfirmsOrDie()
        {
            throw new NotImplementedException();
        }

        public void WaitForConfirmsOrDie(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public int ChannelNumber { get; }

        public string BasicConsume(string queue, bool noAck, IBasicConsumer consumer)
        {
            return BasicConsume(queue: queue, noAck: noAck, consumerTag: Guid.NewGuid().ToString(), noLocal: true, exclusive: false, arguments: null, consumer: consumer);
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, IBasicConsumer consumer)
        {
            return BasicConsume(queue: queue, noAck: noAck, consumerTag: consumerTag, noLocal: true, exclusive: false, arguments: null, consumer: consumer);
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, IDictionary<string, object> arguments, IBasicConsumer consumer)
        {
            return BasicConsume(queue: queue, noAck: noAck, consumerTag: consumerTag, noLocal: true, exclusive: false, arguments: arguments, consumer: consumer);
        }

        private readonly ConcurrentDictionary<string, IBasicConsumer> _consumers = new ConcurrentDictionary<string, IBasicConsumer>();

        public string BasicConsume(string queue, bool noAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments, IBasicConsumer consumer)
        {
            _server.Queues.TryGetValue(queue, out var queueInstance);

            if (queueInstance != null)
            {
                Func<string, IBasicConsumer, IBasicConsumer> updateFunction = (s, basicConsumer) => basicConsumer;
                _consumers.AddOrUpdate(consumerTag, consumer, updateFunction);

                NotifyConsumerOfExistingMessages(consumerTag, consumer, queueInstance);
                NotifyConsumerWhenMessagesAreReceived(consumerTag, consumer, queueInstance);
            }

            return consumerTag;
        }

        private void NotifyConsumerWhenMessagesAreReceived(string consumerTag, IBasicConsumer consumer, Queue queueInstance)
        {
            queueInstance.MessagePublished += (sender, message) => { NotifyConsumerOfMessage(consumerTag, consumer, message); };
        }

        private void NotifyConsumerOfExistingMessages(string consumerTag, IBasicConsumer consumer, Queue queueInstance)
        {
            foreach (var message in queueInstance.Messages)
            {
                NotifyConsumerOfMessage(consumerTag, consumer, message);
            }
        }

        private void NotifyConsumerOfMessage(string consumerTag, IBasicConsumer consumer, RabbitMessage message)
        {
            Interlocked.Increment(ref _lastDeliveryTag);
            var deliveryTag = Convert.ToUInt64(_lastDeliveryTag);
            const bool redelivered = false;
            var exchange = message.Exchange;
            var routingKey = message.RoutingKey;
            var basicProperties = message.BasicProperties ?? CreateBasicProperties();
            var body = message.Body;

            Func<ulong, RabbitMessage, RabbitMessage> updateFunction = (key, existingMessage) => existingMessage;
            WorkingMessages.AddOrUpdate(deliveryTag, message, updateFunction);

            consumer.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, basicProperties, body);
        }

        public void BasicCancel(string consumerTag)
        {
            _consumers.TryRemove(consumerTag, out var consumer);

            consumer?.HandleBasicCancelOk(consumerTag);
        }

        private long _lastDeliveryTag;
        public readonly ConcurrentDictionary<ulong, RabbitMessage> WorkingMessages = new ConcurrentDictionary<ulong, RabbitMessage>();

        public BasicGetResult BasicGet(string queue, bool autoAck)
        {
            _server.Queues.TryGetValue(queue, out var queueInstance);
            if (queueInstance == null) return null;

            RabbitMessage message;
            if (autoAck)
            {
                queueInstance.Messages.TryDequeue(out message);
            }
            else
            {
                queueInstance.Messages.TryPeek(out message);
            }

            if (message == null)
                return null;

            Interlocked.Increment(ref _lastDeliveryTag);
            var deliveryTag = Convert.ToUInt64(_lastDeliveryTag);
            const bool redelivered = false;
            var exchange = message.Exchange;
            var routingKey = message.RoutingKey;
            var messageCount = Convert.ToUInt32(queueInstance.Messages.Count);
            var basicProperties = message.BasicProperties ?? CreateBasicProperties();
            var body = message.Body;

            if (autoAck)
            {
                WorkingMessages.TryRemove(deliveryTag, out _);
            }
            else
            {
                RabbitMessage UpdateFunction(ulong key, RabbitMessage existingMessage) => existingMessage;
                WorkingMessages.AddOrUpdate(deliveryTag, message, UpdateFunction);
            }

            return new BasicGetResult(deliveryTag, redelivered, exchange, routingKey, messageCount, basicProperties, body);
        }

        public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            PrefetchSize = prefetchSize;
            PrefetchCount = prefetchCount;
            ApplyPrefetchToAllChannels = global;
        }

        public void BasicPublish(PublicationAddress addr, IBasicProperties basicProperties, byte[] body)
        {
            BasicPublish(exchange: addr.ExchangeName, routingKey: addr.RoutingKey, mandatory: true, immediate: true, basicProperties: basicProperties, body: body);
        }

        public void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, byte[] body)
        {
            BasicPublish(exchange: exchange, routingKey: routingKey, mandatory: true, immediate: true, basicProperties: basicProperties, body: body);
        }

        public void BasicPublish(string exchange, string routingKey, bool mandatory, IBasicProperties basicProperties, byte[] body)
        {
            BasicPublish(exchange: exchange, routingKey: routingKey, mandatory: mandatory, immediate: true, basicProperties: basicProperties, body: body);
        }

        public void BasicPublish(string exchange, string routingKey, bool mandatory, bool immediate, IBasicProperties basicProperties, byte[] body)
        {
            var parameters = new RabbitMessage
            {
                Exchange = exchange,
                RoutingKey = routingKey,
                Mandatory = mandatory,
                Immediate = immediate,
                BasicProperties = basicProperties,
                Body = body
            };

            Func<string, Exchange> addExchange = s =>
            {
                var newExchange = new Exchange
                {
                    Name = exchange,
                    Arguments = null,
                    IsAutoDelete = false,
                    IsDurable = false,
                    Type = "direct"
                };
                newExchange.PublishMessage(parameters);

                return newExchange;
            };
            Func<string, Exchange, Exchange> updateExchange = (s, existingExchange) =>
            {
                existingExchange.PublishMessage(parameters);

                return existingExchange;
            };
            _server.Exchanges.AddOrUpdate(exchange, addExchange, updateExchange);

            NextPublishSeqNo++;
        }

        public void BasicAck(ulong deliveryTag, bool multiple)
        {
            if (multiple)
            {
                while (BasicAckSingle(deliveryTag))
                    --deliveryTag;
            }
            else
            {
                BasicAckSingle(deliveryTag);
            }
        }

        private bool BasicAckSingle(ulong deliveryTag)
        {
            WorkingMessages.TryRemove(deliveryTag, out var message);

            if (message == null) return false;

            _server.Queues.TryGetValue(message.Queue, out var queue);

            queue?.Messages.TryDequeue(out message);

            return message != null;
        }

        public void BasicReject(ulong deliveryTag, bool requeue)
        {
            BasicNack(deliveryTag: deliveryTag, multiple: false, requeue: requeue);
        }

        public void BasicNack(ulong deliveryTag, bool multiple, bool requeue)
        {
            if (requeue) return;

            foreach (var queue in WorkingMessages.Select(m => m.Value.Queue))
            {
                _server.Queues.TryGetValue(queue, out var queueInstance);

                if (queueInstance != null)
                {
                    queueInstance.Messages = new ConcurrentQueue<RabbitMessage>();
                }
            }

            WorkingMessages.TryRemove(deliveryTag, out var message);
            if (message == null) return;

            // As per the RabbitMQ spec, we need to check if this message should be delivered to a Dead Letter Exchange (DLX) if:
            // 1) The message was NAcked or Rejected
            // 2) Requeue = false
            // See: https://www.rabbitmq.com/dlx.html
            _server.Queues.TryGetValue(message.Queue, out var processingQueue);
            if
            (
                processingQueue.Arguments != null
                    && processingQueue.Arguments.TryGetValue("x-dead-letter-exchange", out var dlx)
                    && _server.Exchanges.TryGetValue((string)dlx, out var exchange)
            )
            {
                // Queue has a DLX and it exists on the server.
                // Publish the message to the DLX.
                exchange.PublishMessage(message);
                return;
            }

            foreach (var workingMessage in WorkingMessages)
            {
                _server.Queues.TryGetValue(workingMessage.Value.Queue, out var queueInstance);

                queueInstance?.PublishMessage(workingMessage.Value);
            }
        }

        public void BasicRecover(bool requeue)
        {
            if (requeue)
            {
                foreach (var message in WorkingMessages)
                {
                    _server.Queues.TryGetValue(message.Value.Queue, out var queueInstance);

                    queueInstance?.PublishMessage(message.Value);
                }
            }

            WorkingMessages.Clear();
        }

        public void BasicRecoverAsync(bool requeue)
        {
            BasicRecover(requeue);
        }

        public void TxSelect()
        {
            throw new NotImplementedException();
        }

        public void TxCommit()
        {
            throw new NotImplementedException();
        }

        public void TxRollback()
        {
            throw new NotImplementedException();
        }

        public void DtxSelect()
        {
            throw new NotImplementedException();
        }

        public void DtxStart(string dtxIdentifier)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            Close(ushort.MaxValue, string.Empty);
        }

        public void Close(ushort replyCode, string replyText)
        {
            IsClosed = true;
            IsOpen = false;
            CloseReason = new ShutdownEventArgs(ShutdownInitiator.Library, replyCode, replyText);

            foreach (var queue in _localQueues.Where(x => x.IsAutoDelete))
            {
                QueueDelete(queue.Name);
            }

            foreach (var exchange in _localExchanges.Where(x => x.IsAutoDelete))
            {
                ExchangeDelete(exchange.Name);
            }
        }

        public void Abort()
        {
            Abort(ushort.MaxValue, string.Empty);
        }

        public void Abort(ushort replyCode, string replyText)
        {
            IsClosed = true;
            IsOpen = false;
            CloseReason = new ShutdownEventArgs(ShutdownInitiator.Library, replyCode, replyText);
        }

        public IBasicConsumer DefaultConsumer { get; set; }

        public ShutdownEventArgs CloseReason { get; set; }

        public bool IsOpen { get; set; }

        public bool IsClosed { get; set; }

        public ulong NextPublishSeqNo { get; set; }
        public TimeSpan ContinuationTimeout { get; set; }

        event EventHandler<BasicAckEventArgs> IModel.BasicAcks
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        event EventHandler<BasicNackEventArgs> IModel.BasicNacks
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        event EventHandler<EventArgs> IModel.BasicRecoverOk
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        event EventHandler<BasicReturnEventArgs> IModel.BasicReturn
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        event EventHandler<CallbackExceptionEventArgs> IModel.CallbackException
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        event EventHandler<FlowControlEventArgs> IModel.FlowControl
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        event EventHandler<ShutdownEventArgs> IModel.ModelShutdown
        {
            add => AddedModelShutDownEvent += value;
            remove => AddedModelShutDownEvent -= value;
        }

        public EventHandler<ShutdownEventArgs> AddedModelShutDownEvent { get; set; }
    }
}