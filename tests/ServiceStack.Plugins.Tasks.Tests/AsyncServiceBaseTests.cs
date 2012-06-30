using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.ServiceInterface;
using System.Threading.Tasks;
using ServiceStack.Service;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.Plugins.Tasks.Tests
{
    public class Hello { }

    public class HelloResponse
    {
        public string Message { get; set; }
    }

    public class HelloService : AsyncServiceBase<Hello>
    {
        protected override Task<object> Run(Hello request)
        {
            return Task.Factory.StartNew<object>(() =>
            {
                return new HelloResponse() { Message = "This was async!" };
            });
        }
    }

    [TestFixture]
    public class AsyncServiceBaseTests
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
                this.Routes.Add<Hello>("/hello");
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
            var response = client.Get<HelloResponse>("/hello");
            Assert.That(response.Message, Is.EqualTo("This was async!"));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Can_retrieve_data_with_default_endpoint(IServiceClient client)
        {
            var response = client.Send<HelloResponse>(new Hello());
            Assert.That(response.Message, Is.EqualTo("This was async!"));
        }
    }
}
