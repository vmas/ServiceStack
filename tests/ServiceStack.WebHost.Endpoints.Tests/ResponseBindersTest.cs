using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class ResponseBindersTest
    {
        [Route("/responsebinder")]
        public class ResponseBinder { }
        public class ResponseBinderResponse
        {
            public bool IsFromResponseBinder { get; set; }
        }

        public class ResponseBinderService : IService<ResponseBinder>
        {
            public object Execute(ResponseBinder request)
            {
                return new ResponseBinderResponse() { IsFromResponseBinder = false };
            }
        }

        private const string ListeningOn = "http://localhost:8082/";

        public class AppHost : AppHostHttpListenerBase
        {
            public AppHost()
                : base("Response binders test", typeof(ResponseBinderService).Assembly)
            {
            }

            public override void Configure(Funq.Container container)
            {
                this.ResponseBinders[typeof(ResponseBinderResponse)] = (service, request, response) =>
                {
                    if (service is ResponseBinderService &&
                        request is ResponseBinder &&
                        response is ResponseBinderResponse)
                        return new ResponseBinderResponse() { IsFromResponseBinder = true };

                    return response;
                };
            }
        }

        AppHost appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new AppHost();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        static IServiceClient[] ServiceClients = 
        {
            new JsonServiceClient(ListeningOn),
            new XmlServiceClient(ListeningOn),
            new JsvServiceClient(ListeningOn)
        };

        [Test, TestCaseSource("ServiceClients")]
        public void Request_and_Response_Filters_are_executed_using_ServiceClient(IServiceClient client)
        {
            var response = client.Send<ResponseBinderResponse>(new ResponseBinder());
            Assert.True(response.IsFromResponseBinder);
        }

        static IRestClient[] RestClients = 
        {
            new JsonServiceClient(ListeningOn),
            new XmlServiceClient(ListeningOn),
            new JsvServiceClient(ListeningOn)
        };

        [Test, TestCaseSource("RestClients")]
        public void Request_and_Response_Filters_are_executed_using_RestClient(IRestClient client)
        {
            var response = client.Get<ResponseBinderResponse>("/responsebinder");
            Assert.True(response.IsFromResponseBinder);
        }
    }
}
