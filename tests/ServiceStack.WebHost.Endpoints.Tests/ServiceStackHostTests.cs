using System;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class ServiceStackHostTests
	{
		[Test]
		public void Can_run_nested_service()
		{
			var host = new TestAppHost();
			host.Init();

			var request = new Nested();
			var response = host.Config.ServiceController.Execute(request) as NestedResponse;

			Assert.That(response, Is.Not.Null);
		}

		[Test]
		public void Can_run_test_service()
		{
			var host = new TestAppHost();
			host.Init();

			var request = new Test();
            var response = host.Config.ServiceController.Execute(request) as TestResponse;

			Assert.That(response, Is.Not.Null);
			Assert.That(response.Foo, Is.Not.Null);
		}
	}
}