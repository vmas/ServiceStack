using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.Common.Extensions;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using System.Threading;
using ServiceStack.Service;
using ServiceStack.WebHost.Endpoints;
using Funq;
using ServiceStack.Common.Web;

namespace ServiceStack.Plugins.Tasks.Tests
{
    [RestService("/users")]
    public class User { }
    public class UserResponse : IHasResponseStatus
    {
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class UserService : RestServiceBase<User>
    {
        public override object OnGet(User request)
        {
            return Task.Factory.StartNew<HttpError>(() =>
            {
                return new HttpError(System.Net.HttpStatusCode.BadRequest, "CanNotExecute", "Failed to execute!");
            });
        }

        public override object OnPost(User request)
        {
            return Task.Factory.StartNew<UserResponse>(() =>
            {
                throw new HttpError(System.Net.HttpStatusCode.BadRequest, "CanNotExecute", "Failed to execute!");
            });
        }

        public override object OnPut(User request)
        {
            return Task.Factory.StartNew<UserResponse>(() =>
            {
                throw new ArgumentException();
            });
        }
    }

    [TestFixture]
    public class ExceptionHandlingTest
    {
        private const string ListeningOn = "http://localhost:82/";

        public class ExceptionHandlingAppHostHttpListener
            : AppHostHttpListenerBase
        {

            public ExceptionHandlingAppHostHttpListener()
                : base("Exception handling tests", typeof(UserService).Assembly) { }

            public override void Configure(Container container)
            {
            }
        }

        ExceptionHandlingAppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new ExceptionHandlingAppHostHttpListener();
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
			//SOAP not supported in HttpListener
			//new Soap11ServiceClient(ServiceClientBaseUri),
			//new Soap12ServiceClient(ServiceClientBaseUri)
        };


        [Test, TestCaseSource("ServiceClients")]
        public void Handles_Returned_Http_Error(IRestClient client)
        {
            try
            {
                client.Get<UserResponse>("/users");
                Assert.Fail("Should not get here");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo("CanNotExecute"));
                Assert.That(ex.StatusCode, Is.EqualTo((int)System.Net.HttpStatusCode.BadRequest));
                Assert.That(ex.Message, Is.EqualTo("CanNotExecute"));
            }
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Handles_Thrown_Http_Error(IRestClient client)
        {
            try
            {
                client.Post<UserResponse>("/users", new User());
                Assert.Fail("Should not get here");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo("CanNotExecute"));
                Assert.That(ex.StatusCode, Is.EqualTo((int)System.Net.HttpStatusCode.BadRequest));
                Assert.That(ex.Message, Is.EqualTo("CanNotExecute"));
            }
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Handles_Normal_Exception(IRestClient client)
        {
            try
            {
                client.Put<UserResponse>("/users", new User());
                Assert.Fail("Should not get here");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo("ArgumentException"));
                Assert.That(ex.StatusCode, Is.EqualTo((int)System.Net.HttpStatusCode.BadRequest));
            }
        }
    }
}
