using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.Messaging;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/queue", "GET", IsOneWay = true)]
    public class Queue
    {
    }

    public class QueueResponse
    {
    }

    public class QueueService : ServiceBase<Queue>
    {
        protected override object Run(Queue request)
        {
            var test = base.RequestContext;
            return null;
        }
    }

    [TestFixture]
    public class AsyncOneWayTests
    {
        private const string ListeningOn = "http://localhost:82/";

        static int called = 0;

        public class QueueAppHost : AppHostHttpListenerBase
        {
            public QueueAppHost()
                : base("Queue service test", typeof(QueueService).Assembly)
            {
            }

            public override void Configure(Funq.Container container)
            {
                var queue = new InMemoryTransientMessageFactory();
                using (var serviceHost = queue.CreateMessageService())
                {
                    serviceHost.RegisterHandler<Queue>(dto =>
                    {
                        called += 1;
                        return new QueueResponse();
                    });

                    serviceHost.Start();
                }

                container.Register<IMessageFactory>(queue);
            }
        }

        QueueAppHost appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new QueueAppHost();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [TestCase("/queue")]
        [TestCase("/json/oneway/queue")]
        public void Does_add_message_to_queue_on_async_one_way_request(string url)
        {
            called = 0;

            var client = new JsonServiceClient(ListeningOn);
            client.Get<QueueResponse>(url);

            Assert.That(called, Is.EqualTo(1));
        }
    }
}
