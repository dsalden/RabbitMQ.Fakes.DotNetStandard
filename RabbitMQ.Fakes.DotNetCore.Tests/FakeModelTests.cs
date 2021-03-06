﻿using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using RabbitMQ.Fakes.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQ.Client.Exceptions;
using Queue = RabbitMQ.Fakes.models.Queue;

namespace RabbitMQ.Fakes.Tests
{
    [TestFixture]
    public class FakeModelTests
    {
        private bool _wasCalled;

        [Test]
        public void AddModelShutDownEvent_EventIsTracked()
        {
            //arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            //act
            Assert.That(model.AddedModelShutDownEvent, Is.Null);
            ((IModel)model).ModelShutdown += (args, e) => { _wasCalled = true; };

            //Assert
            Assert.That(model.AddedModelShutDownEvent, Is.Not.Null);
        }

        [Test]
        public void AddModelShutDownEvent_EventIsRemoved()
        {
            //arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);
            EventHandler<ShutdownEventArgs> onModelShutdown = (args, e) => { _wasCalled = true; };
            ((IModel)model).ModelShutdown += onModelShutdown;

            //act
            Assert.That(model.AddedModelShutDownEvent, Is.Not.Null);
            ((IModel)model).ModelShutdown -= onModelShutdown;

            //Assert
            Assert.That(model.AddedModelShutDownEvent, Is.Null);
        }

        [Test]
        public void CreateBasicProperties_ReturnsBasicProperties()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            // Act
            var result = model.CreateBasicProperties();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ChannelFlow_SetsIfTheChannelIsActive(bool value)
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            // Act
            model.ChannelFlow(value);

            // Assert
            Assert.That(model.IsChannelFlowActive, Is.EqualTo(value));
        }

        [Test]
        public void ExchangeDeclare_AllArguments_CreatesExchange()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";
            const string exchangeType = "someType";
            const bool isDurable = true;
            const bool isAutoDelete = false;
            var arguments = new Dictionary<string, object>();

            // Act
            model.ExchangeDeclare(exchange: exchangeName, type: exchangeType, durable: isDurable, autoDelete: isAutoDelete, arguments: arguments);

            // Assert
            Assert.That(node.Exchanges, Has.Count.EqualTo(1));

            var exchange = node.Exchanges.First();
            AssertExchangeDetails(exchange, exchangeName, isAutoDelete, arguments, isDurable, exchangeType);
        }

        [Test]
        public void ExchangeDeclare_WithNameTypeAndDurable_CreatesExchange()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";
            const string exchangeType = "someType";
            const bool isDurable = true;

            // Act
            model.ExchangeDeclare(exchange: exchangeName, type: exchangeType, durable: isDurable);

            // Assert
            Assert.That(node.Exchanges, Has.Count.EqualTo(1));

            var exchange = node.Exchanges.First();
            AssertExchangeDetails(exchange, exchangeName, false, null, isDurable, exchangeType);
        }

        [Test]
        public void ExchangeDeclare_WithNameType_CreatesExchange()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";
            const string exchangeType = "someType";

            // Act
            model.ExchangeDeclare(exchange: exchangeName, type: exchangeType);

            // Assert
            Assert.That(node.Exchanges, Has.Count.EqualTo(1));

            var exchange = node.Exchanges.First();
            AssertExchangeDetails(exchange, exchangeName, false, null, false, exchangeType);
        }

        [Test]
        public void ExchangeDeclare_WithAutoDelete_CreatesExchange()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";
            const string exchangeType = "someType";
            const bool isDurable = true;
            const bool isAutoDelete = true;
            var arguments = new Dictionary<string, object>();

            // Act
            model.ExchangeDeclare(exchange: exchangeName, type: exchangeType, durable: isDurable, autoDelete: isAutoDelete, arguments: arguments);

            model.Close();

            // Assert
            Assert.That(node.Exchanges, Has.Count.EqualTo(0));
        }

        [Test]
        public void ExchangeDeclarePassive_WithName_CreatesExchange()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";

            // Act
            model.ExchangeDeclarePassive(exchange: exchangeName);

            // Assert
            Assert.That(node.Exchanges, Has.Count.EqualTo(1));

            var exchange = node.Exchanges.First();
            AssertExchangeDetails(exchange, exchangeName, false, null, false, null);
        }

        [Test]
        public void ExchangeDeclareNoWait_CreatesExchange()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";
            const string exchangeType = "someType";
            const bool isDurable = true;
            const bool isAutoDelete = false;
            var arguments = new Dictionary<string, object>();

            // Act
            model.ExchangeDeclareNoWait(exchange: exchangeName, type: exchangeType, durable: isDurable, autoDelete: isAutoDelete, arguments: arguments);

            // Assert
            Assert.That(node.Exchanges, Has.Count.EqualTo(1));

            var exchange = node.Exchanges.First();
            AssertExchangeDetails(exchange, exchangeName, isAutoDelete, arguments, isDurable, exchangeType);
        }

        private static void AssertExchangeDetails(KeyValuePair<string, Exchange> exchange, string exchangeName, bool isAutoDelete, IDictionary<string, object> arguments, bool isDurable, string exchangeType)
        {
            Assert.That(exchange.Key, Is.EqualTo(exchangeName));
            Assert.That(exchange.Value.IsAutoDelete, Is.EqualTo(isAutoDelete));
            Assert.That(exchange.Value.Arguments, Is.EqualTo(arguments));
            Assert.That(exchange.Value.IsDurable, Is.EqualTo(isDurable));
            Assert.That(exchange.Value.Name, Is.EqualTo(exchangeName));
            Assert.That(exchange.Value.Type, Is.EqualTo(exchangeType));
        }

        [Test]
        public void ExchangeDelete_NameOnlyExchangeExists_RemovesTheExchange()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";
            model.ExchangeDeclare(exchangeName, "someType");

            // Act
            model.ExchangeDelete(exchange: exchangeName);

            // Assert
            Assert.That(node.Exchanges, Has.Count.EqualTo(0));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ExchangeDelete_ExchangeExists_RemovesTheExchange(bool ifUnused)
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";
            model.ExchangeDeclare(exchangeName, "someType");

            // Act
            model.ExchangeDelete(exchange: exchangeName, ifUnused: ifUnused);

            // Assert
            Assert.That(node.Exchanges, Has.Count.EqualTo(0));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ExchangeDeleteNoWait_ExchangeExists_RemovesTheExchange(bool ifUnused)
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";
            model.ExchangeDeclare(exchangeName, "someType");

            // Act
            model.ExchangeDeleteNoWait(exchange: exchangeName, ifUnused: ifUnused);

            // Assert
            Assert.That(node.Exchanges, Has.Count.EqualTo(0));
        }

        [Test]
        public void ExchangeDelete_ExchangeDoesNotExists_DoesNothing()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";
            model.ExchangeDeclare(exchangeName, "someType");

            // Act
            model.ExchangeDelete(exchange: "someOtherExchange");

            // Assert
            Assert.That(node.Exchanges, Has.Count.EqualTo(1));
        }

        [Test]
        public void ExchangeBind_BindsAnExchangeToAQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someQueue";
            const string exchangeName = "someExchange";
            const string routingKey = "someRoutingKey";
            var arguments = new Dictionary<string, object>();

            model.ExchangeDeclare(exchangeName, "direct");
            model.QueueDeclarePassive(queueName);

            // Act
            model.ExchangeBind(queueName, exchangeName, routingKey, arguments);

            // Assert
            AssertBinding(node, exchangeName, routingKey, queueName);
        }

        [Test]
        public void QueueBind_BindsAnExchangeToAQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someQueue";
            const string exchangeName = "someExchange";
            const string routingKey = "someRoutingKey";
            var arguments = new Dictionary<string, object>();

            model.ExchangeDeclare(exchangeName, "direct");
            model.QueueDeclarePassive(queueName);

            // Act
            model.QueueBind(queueName, exchangeName, routingKey, arguments);

            // Assert
            AssertBinding(node, exchangeName, routingKey, queueName);
        }

        private static void AssertBinding(RabbitServer server, string exchangeName, string routingKey, string queueName)
        {
            Assert.That(server.Exchanges[exchangeName].Bindings, Has.Count.EqualTo(1));
            Assert.That(server.Exchanges[exchangeName].Bindings.First().Value.RoutingKey, Is.EqualTo(routingKey));
            Assert.That(server.Exchanges[exchangeName].Bindings.First().Value.Queue.Name, Is.EqualTo(queueName));
        }

        [Test]
        public void ExchangeUnbind_RemovesBinding()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someQueue";
            const string exchangeName = "someExchange";
            const string routingKey = "someRoutingKey";
            var arguments = new Dictionary<string, object>();

            model.ExchangeDeclare(exchangeName, "direct");
            model.QueueDeclarePassive(queueName);
            model.ExchangeBind(exchangeName, queueName, routingKey, arguments);

            // Act
            model.ExchangeUnbind(queueName, exchangeName, routingKey, arguments);

            // Assert
            Assert.That(node.Exchanges[exchangeName].Bindings, Is.Empty);
            Assert.That(node.Queues[queueName].Bindings, Is.Empty);
        }

        [Test]
        public void QueueUnbind_RemovesBinding()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someQueue";
            const string exchangeName = "someExchange";
            const string routingKey = "someRoutingKey";
            var arguments = new Dictionary<string, object>();

            model.ExchangeDeclare(exchangeName, "direct");
            model.QueueDeclarePassive(queueName);
            model.ExchangeBind(exchangeName, queueName, routingKey, arguments);

            // Act
            model.QueueUnbind(queueName, exchangeName, routingKey, arguments);

            // Assert
            Assert.That(node.Exchanges[exchangeName].Bindings, Is.Empty);
            Assert.That(node.Queues[queueName].Bindings, Is.Empty);
        }

        [Test]
        public void QueueDeclare_NoArguments_CreatesQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            // Act
            model.QueueDeclare();

            // Assert
            Assert.That(node.Queues, Has.Count.EqualTo(1));
        }

        [Test]
        public void QueueDeclarePassive_WithName_CreatesQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "myQueue";

            // Act
            model.QueueDeclarePassive(queueName);

            // Assert
            Assert.That(node.Queues, Has.Count.EqualTo(1));
            Assert.That(node.Queues.First().Key, Is.EqualTo(queueName));
            Assert.That(node.Queues.First().Value.Name, Is.EqualTo(queueName));
        }

        [Test]
        public void QueueDeclare_CreatesQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someQueue";
            const bool isDurable = true;
            const bool isExclusive = true;
            const bool isAutoDelete = false;
            var arguments = new Dictionary<string, object>();

            // Act
            model.QueueDeclare(queue: queueName, durable: isDurable, exclusive: isExclusive, autoDelete: isAutoDelete, arguments: arguments);

            // Assert
            Assert.That(node.Queues, Has.Count.EqualTo(1));

            var queue = node.Queues.First();
            AssertQueueDetails(queue, queueName, isAutoDelete, arguments, isDurable, isExclusive);
        }

        [Test]
        public void QueueDeclare_WithAutoDelete_CreatesQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someQueue";
            const bool isDurable = true;
            const bool isExclusive = true;
            const bool isAutoDelete = true;
            var arguments = new Dictionary<string, object>();

            // Act
            model.QueueDeclare(queue: queueName, durable: isDurable, exclusive: isExclusive, autoDelete: isAutoDelete, arguments: arguments);
            model.Close();

            // Assert
            Assert.That(node.Queues, Has.Count.EqualTo(0));
        }

        [Test]
        public void QueueDeclare_WithExclusive_SameModel_CreatesQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someQueue";
            const bool isDurable = true;
            const bool isExclusive = true;
            const bool isAutoDelete = true;
            var arguments = new Dictionary<string, object>();

            // Act
            model.QueueDeclare(queue: queueName, durable: isDurable, exclusive: isExclusive, autoDelete: isAutoDelete, arguments: arguments);
            model.QueueDeclare(queue: queueName, durable: isDurable, exclusive: isExclusive, autoDelete: isAutoDelete, arguments: arguments);

            // Assert
            Assert.That(node.Queues, Has.Count.EqualTo(1));

            var queue = node.Queues.First();
            AssertQueueDetails(queue, queueName, isAutoDelete, arguments, isDurable, isExclusive);
        }

        [Test]
        public void QueueDeclare_WithExclusive_DifferentModel_CreatesQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);
            var model2 = new FakeModel(node);

            const string queueName = "someQueue";
            const bool isDurable = true;
            const bool isExclusive = true;
            const bool isAutoDelete = true;
            var arguments = new Dictionary<string, object>();

            // Act
            model.QueueDeclare(queue: queueName, durable: isDurable, exclusive: isExclusive, autoDelete: isAutoDelete, arguments: arguments);
            var ex = Assert.Throws<OperationInterruptedException>(
                delegate { model2.QueueDeclare(queue: queueName, durable: isDurable, exclusive: isExclusive, autoDelete: isAutoDelete, arguments: arguments); });

            // Assert
            Assert.That(ex.ShutdownReason.ReplyCode, Is.EqualTo(405));
            Assert.That(ex.ShutdownReason.Initiator, Is.EqualTo(ShutdownInitiator.Peer));
            StringAssert.StartsWith($"RESOURCE_LOCKED - cannot obtain exclusive access to locked queue '{queueName}'", ex.ShutdownReason.ReplyText);
        }

        private static void AssertQueueDetails(KeyValuePair<string, Queue> queue, string exchangeName, bool isAutoDelete, Dictionary<string, object> arguments, bool isDurable, bool isExclusive)
        {
            Assert.That(queue.Key, Is.EqualTo(exchangeName));
            Assert.That(queue.Value.IsAutoDelete, Is.EqualTo(isAutoDelete));
            Assert.That(queue.Value.Arguments, Is.EqualTo(arguments));
            Assert.That(queue.Value.IsDurable, Is.EqualTo(isDurable));
            Assert.That(queue.Value.Name, Is.EqualTo(exchangeName));
            Assert.That(queue.Value.IsExclusive, Is.EqualTo(isExclusive));
        }

        [Test]
        public void QueueDelete_NameOnly_DeletesTheQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someName";
            model.QueueDeclare(queueName, true, true, true, null);

            // Act
            model.QueueDelete(queueName);

            // Assert
            Assert.That(node.Queues, Is.Empty);
        }

        [Test]
        public void QueueDelete_WithArguments_DeletesTheQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someName";
            model.QueueDeclare(queueName, true, true, true, null);

            // Act
            model.QueueDelete(queueName, true, true);

            // Assert
            Assert.That(node.Queues, Is.Empty);
        }

        [Test]
        public void QueueDeleteNoWait_WithArguments_DeletesTheQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someName";
            model.QueueDeclare(queueName, true, true, true, null);

            // Act
            model.QueueDeleteNoWait(queueName, true, true);

            // Assert
            Assert.That(node.Queues, Is.Empty);
        }

        [Test]
        public void QueueDelete_NonExistentQueue_DoesNothing()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            // Act
            model.QueueDelete("someQueue");

            // Assert
            Assert.That(node.Queues, Is.Empty);
        }

        [Test]
        public void QueuePurge_RemovesAllMessagesFromQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.QueueDeclarePassive("my_other_queue");
            node.Queues["my_other_queue"].Messages.Enqueue(new RabbitMessage());
            node.Queues["my_other_queue"].Messages.Enqueue(new RabbitMessage());

            model.QueueDeclarePassive("my_queue");
            node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage());
            node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage());
            node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage());
            node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage());

            // Act
            model.QueuePurge("my_queue");

            // Assert
            Assert.That(node.Queues["my_queue"].Messages, Is.Empty);
            Assert.That(node.Queues["my_other_queue"].Messages, Is.Not.Empty);
        }

        [Test]
        public void Close_ClosesTheChannel()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            // Act
            model.Close();

            // Assert
            Assert.That(model.IsClosed, Is.True);
            Assert.That(model.IsOpen, Is.False);
            Assert.That(model.CloseReason, Is.Not.Null);
        }

        [Test]
        public void Close_WithArguments_ClosesTheChannel()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            // Act
            model.Close(5, "some message");

            // Assert
            Assert.That(model.IsClosed, Is.True);
            Assert.That(model.IsOpen, Is.False);
            Assert.That(model.CloseReason, Is.Not.Null);
        }

        [Test]
        public void Abort_ClosesTheChannel()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            // Act
            model.Abort();

            // Assert
            Assert.That(model.IsClosed, Is.True);
            Assert.That(model.IsOpen, Is.False);
            Assert.That(model.CloseReason, Is.Not.Null);
        }

        [Test]
        public void Abort_WithArguments_ClosesTheChannel()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            // Act
            model.Abort(5, "some message");

            // Assert
            Assert.That(model.IsClosed, Is.True);
            Assert.That(model.IsOpen, Is.False);
            Assert.That(model.CloseReason, Is.Not.Null);
        }

        [Test]
        public void BasicPublish_PublishesMessage()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            model.QueueDeclarePassive("my_queue");
            model.ExchangeBind("my_queue", "my_exchange", null);

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);

            // Act
            model.BasicPublish("my_exchange", null, new BasicProperties(), encodedMessage);

            // Assert
            Assert.That(node.Queues["my_queue"].Messages.Count, Is.EqualTo(1));
            Assert.That(node.Queues["my_queue"].Messages.First().Body, Is.EqualTo(encodedMessage));
        }

        [Test]
        public void BasicAck()
        {
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            model.QueueDeclarePassive("my_queue");
            model.ExchangeBind("my_queue", "my_exchange", null);

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);
            model.BasicPublish("my_exchange", null, new BasicProperties(), encodedMessage);
            model.BasicConsume("my_queue", true, new EventingBasicConsumer(model));

            // Act
            var deliveryTag = model.WorkingMessages.First().Key;
            model.BasicAck(deliveryTag, false);

            // Assert
            Assert.That(node.Queues["my_queue"].Messages.Count, Is.EqualTo(0));
        }

        [Test]
        public void BasicAckMultiple()
        {
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            model.QueueDeclarePassive("my_queue");
            model.ExchangeBind("my_queue", "my_exchange", null);

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);
            model.BasicPublish("my_exchange", null, new BasicProperties(), encodedMessage);
            model.BasicConsume("my_queue", true, new EventingBasicConsumer(model));

            var message2 = "hello world, too!!";
            var encodedMessage2 = Encoding.ASCII.GetBytes(message2);
            model.BasicPublish("my_exchange", null, new BasicProperties(), encodedMessage2);
            model.BasicConsume("my_queue", true, new EventingBasicConsumer(model));

            // Act
            var deliveryTag = model.WorkingMessages.Last().Key;
            model.BasicAck(deliveryTag, true);

            // Assert
            Assert.That(node.Queues["my_queue"].Messages.Count, Is.EqualTo(0));
        }

        [Test]
        public void BasicGet_MessageOnQueue_GetsMessage()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            model.QueueDeclarePassive("my_queue");
            model.ExchangeBind("my_queue", "my_exchange", null);

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);
            model.BasicPublish("my_exchange", null, new BasicProperties(), encodedMessage);

            // Act
            var response = model.BasicGet("my_queue", false);

            // Assert
            Assert.That(response.Body, Is.EqualTo(encodedMessage));
            Assert.That(response.DeliveryTag, Is.GreaterThan(0));
        }

        [Test]
        public void BasicGet_NoMessageOnQueue_ReturnsNull()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.QueueDeclarePassive("my_queue");

            // Act
            var response = model.BasicGet("my_queue", false);

            // Assert
            Assert.That(response, Is.Null);
        }

        [Test]
        public void BasicGet_NoQueue_ReturnsNull()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            // Act
            var response = model.BasicGet("my_queue", false);

            // Assert
            Assert.That(response, Is.Null);
        }

        [TestCase(true, 1, TestName = "If requeue param to BasicNack is true, the message that is nacked should remain in Rabbit")]
        [TestCase(false, 0, TestName = "If requeue param to BasicNack is false, the message that is nacked should be removed from Rabbit")]
        public void Nacking_Message_Should_Not_Reenqueue_Brand_New_Message(bool requeue, int expectedMessageCount)
        {
            // arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            model.QueueDeclare("my_queue");
            model.ExchangeBind("my_queue", "my_exchange", null);

            var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
            model.BasicPublish("my_exchange", null, new BasicProperties(), encodedMessage);
            model.BasicConsume("my_queue", false, new EventingBasicConsumer(model));

            // act
            var deliveryTag = model.WorkingMessages.First().Key;
            model.BasicNack(deliveryTag, false, requeue);

            // assert
            Assert.That(node.Queues["my_queue"].Messages.Count, Is.EqualTo(expectedMessageCount));
            Assert.That(model.WorkingMessages.Count, Is.EqualTo(expectedMessageCount));
        }

        [TestCase(true, 0, TestName = "BasicGet WITH auto-ack SHOULD remove the message from the queue")]
        [TestCase(false, 1, TestName = "BasicGet with NO auto-ack should NOT remove the message from the queue")]
        public void BasicGet_Should_Not_Remove_The_Message_From_Queue_If_Not_Acked(bool autoAck, int expectedMessageCount)
        {
            // arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            model.QueueDeclare("my_queue");
            model.ExchangeBind("my_queue", "my_exchange", null);

            var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
            model.BasicPublish("my_exchange", null, new BasicProperties(), encodedMessage);

            // act
            var message = model.BasicGet("my_queue", autoAck);

            // assert
            Assert.That(node.Queues["my_queue"].Messages.Count, Is.EqualTo(expectedMessageCount));
            Assert.That(model.WorkingMessages.Count, Is.EqualTo(expectedMessageCount));
        }

        [Test]
        public void BasicRejectDLX()
        {
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.ExchangeDeclare("dead_letter_exchange", ExchangeType.Direct);
            model.QueueDeclare("error_queue");
            model.ExchangeBind("error_queue", "dead_letter_exchange", null);

            model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            model.QueueDeclare("my_queue", arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "dead_letter_exchange" },
                { "x-dead-letter-routing-key", "error_queue" }
            });
            model.ExchangeBind("my_queue", "my_exchange", null);

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);
            node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage() { Queue = "my_queue", Body = encodedMessage });
            var rabbitMessage = model.BasicGet("my_queue", false);

            // Act
            model.BasicReject(rabbitMessage.DeliveryTag, requeue: false);

            // Assert
            Assert.That(node.Queues["my_queue"].Messages.Count, Is.EqualTo(0));
            Assert.That(node.Queues["error_queue"].Messages.Count, Is.EqualTo(1));
        }

        [Test]
        public void BasicRejectNoDLX()
        {
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.ExchangeDeclare("dead_letter_exchange", ExchangeType.Direct);
            model.QueueDeclare("error_queue");
            model.ExchangeBind("error_queue", "dead_letter_exchange", null);

            model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            model.QueueDeclare("my_queue");
            model.ExchangeBind("my_queue", "my_exchange", null);

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);
            node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage() { Queue = "my_queue", Body = encodedMessage });
            var rabbitMessage = model.BasicGet("my_queue", false);

            // Act
            model.BasicReject(rabbitMessage.DeliveryTag, requeue: false);

            // Assert
            Assert.That(node.Queues["my_queue"].Messages.Count, Is.EqualTo(0));
            Assert.That(node.Queues["error_queue"].Messages.Count, Is.EqualTo(0));
        }
    }
}