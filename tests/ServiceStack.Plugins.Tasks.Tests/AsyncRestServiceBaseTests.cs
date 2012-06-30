using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceInterface;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.Plugins.Tasks.Tests
{
    public class Bye { }

    public class ByeResponse
    {
        public string Message { get; set; }
    }

    public class ByeService : AsyncRestServiceBase<Bye>
    {
        public override Task<object> OnPost(Bye request)
        {
            return Task.Factory.StartNew<object>(() =>
            {
                return new ByeResponse() { Message = "This was async!" };
            });
        }
    }

    [TestFixture]
    public class AsyncRestServiceBaseTests
    {
        const string ListeningOn = "http://localhost:82/";

        class AppHostHttpListener : AppHostHttpListenerBase
        {
            public AppHostHttpListener()
                : base("Async service tests", typeof(AsyncService).Assembly)
            {
            }

            public override void Configure(Funq.Container container)
            {
                this.LoadPlugin(new TaskSupport());
                this.Routes.Add<Bye>("/bye");
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
            var response = client.Post<ByeResponse>("/bye", new Bye());
            Assert.That(response.Message, Is.EqualTo("This was async!"));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Can_retrieve_data_with_default_endpoint(IServiceClient client)
        {
            var response = client.Send<ByeResponse>(new Bye());
            Assert.That(response.Message, Is.EqualTo("This was async!"));
        }
    }
}
