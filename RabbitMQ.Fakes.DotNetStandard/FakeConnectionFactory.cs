using RabbitMQ.Client;
using System.Collections.Generic;

namespace RabbitMQ.Fakes
{
    public class FakeConnectionFactory : ConnectionFactory
    {
        public IConnection Connection { get; private set; }
        public RabbitServer Server { get; private set; }

        public FakeConnectionFactory() : this(new RabbitServer())
        {
        }

        public FakeConnectionFactory(RabbitServer server)
        {
            Server = server;
        }

        public FakeConnectionFactory WithConnection(IConnection connection)
        {
            Connection = connection;
            return this;
        }

        public FakeConnectionFactory WithRabbitServer(RabbitServer server)
        {
            Server = server;
            return this;
        }

        public FakeConnection UnderlyingConnection => (FakeConnection)Connection;

        public List<FakeModel> UnderlyingModel
        {
            get
            {
                var connection = UnderlyingConnection;

                return connection?.Models;
            }
        }

        public override IConnection CreateConnection()
        {
            return Connection ?? (Connection = new FakeConnection(Server));
        }
    }
}