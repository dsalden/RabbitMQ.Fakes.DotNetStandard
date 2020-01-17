using RabbitMQ.Fakes.models;
using System.Collections.Concurrent;

namespace RabbitMQ.Fakes
{
    public class RabbitServer
    {
        public ConcurrentDictionary<string, Exchange> Exchanges = new ConcurrentDictionary<string, Exchange>();
        public ConcurrentDictionary<string, Queue> Queues = new ConcurrentDictionary<string, Queue>();

        public void Reset()
        {
            Exchanges.Clear();
            Queues.Clear();
        }
    }
}