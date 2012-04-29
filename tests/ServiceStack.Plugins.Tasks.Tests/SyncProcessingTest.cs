using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.Plugins.Tasks.Tests
{
    public class Sync { }
    public class SyncResponse
    {
        public string Message { get; set; }
    }

    public class SyncService : IService<Sync>
    {
        public object Execute(Sync request)
        {
            return new SyncResponse() { Message = "This was sync!" };
        }
    }

    [TestFixture]
    public class SyncProcessingTest
    {
        const string ListeningOn = "http://localhost:82/";

        class AppHostHttpListener : AppHostHttpListenerBase
        {
            public AppHostHttpListener()
                : base("Sync service tests", typeof(SyncService).Assembly)
            {
            }

            public override void Configure(Funq.Container container)
            {
                this.LoadPlugin(new TaskSupport());
                this.Routes.Add<Sync>("/sync");
            }
        }

        AppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new AppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        static IRestClient[] ServiceClients = 
        {
            new JsonServiceClient(ListeningOn),
            new XmlServiceClient(ListeningOn),
            new JsvServiceClient(ListeningOn)
        };

        [Test, TestCaseSource("ServiceClients")]
        public void Can_retrieve_data_with_rest_endpoint(IRestClient client)
        {
            var response = client.Get<SyncResponse>("/sync");
            Assert.That(response.Message, Is.EqualTo("This was sync!"));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Can_retrieve_data_with_default_endpoint(IServiceClient client)
        {
            var response = client.Send<SyncResponse>(new Sync());
            Assert.That(response.Message, Is.EqualTo("This was sync!"));
        }
    }
}
