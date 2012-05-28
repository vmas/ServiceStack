using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.Common.Extensions;
using ServiceStack.Service;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public abstract class SyncServiceClientTests
    {
        protected const string ListeningOn = "http://localhost:85/";

        ExampleAppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new ExampleAppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            if (appHost == null) return;
            appHost.Dispose();
            appHost = null;
        }

        protected abstract IServiceClient CreateServiceClient();

        [Test]
        public void Can_call_Send_on_ServiceClient()
        {
            var serviceClient = CreateServiceClient();

            var request = new GetFactorial { ForNumber = 3 };
            var response = serviceClient.Send<GetFactorialResponse>(request);

            Assert.That(response, Is.Not.Null, "No response received");
            Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(request.ForNumber)));
        }

        [TestFixture]
        public class JsonSyncServiceClientTests : SyncServiceClientTests
        {
            protected override IServiceClient CreateServiceClient()
            {
                return new JsonServiceClient(ListeningOn);
            }
        }

        [TestFixture]
        public class JsvSyncServiceClientTests : SyncServiceClientTests
        {
            protected override IServiceClient CreateServiceClient()
            {
                return new JsvServiceClient(ListeningOn);
            }
        }

        [TestFixture]
        public class XmlSyncServiceClientTests : SyncServiceClientTests
        {
            protected override IServiceClient CreateServiceClient()
            {
                return new XmlServiceClient(ListeningOn);
            }
        }
    }
}
