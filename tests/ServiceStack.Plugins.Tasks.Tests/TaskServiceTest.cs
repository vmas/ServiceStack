using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;
using System.Threading.Tasks;
using System.Threading;
using ServiceStack.WebHost.Endpoints;
using NUnit.Framework;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.Plugins.Tasks.Tests
{
	public class Async { }
	public class AsyncResponse
	{
		public string Message { get; set; }
	}

	public class AsyncService : IService<Async>
	{
		public object Execute(Async request)
		{
			return Task.Factory.StartNew<AsyncResponse>(() =>
			{
				return new AsyncResponse() { Message = "This was async!" };
			});
		}
	}

	[TestFixture]
	public class TaskServiceTest
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
				this.Routes.Add<Async>("/async");
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
			var response = client.Get<AsyncResponse>("/async");
			Assert.That(response.Message, Is.EqualTo("This was async!"));
		}

		[Test, TestCaseSource("ServiceClients")]
		public void Can_retireve_data_with_default_endpoint(IServiceClient client)
		{
			var response = client.Send<AsyncResponse>(new Async());
			Assert.That(response.Message, Is.EqualTo("This was async!"));
		}
	}
}
