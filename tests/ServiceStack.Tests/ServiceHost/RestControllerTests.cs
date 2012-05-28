using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.ServiceHost;

namespace ServiceStack.Tests.ServiceHost
{
	[TestFixture]
	public class RestControllerTests
	{
		[RestService("/hello")]
		class RequestDto { }

		[Test]
		public void Does_notice_RestService_attributes()
		{
			IRestController controller = new RestController();
			controller.RegisterRestPaths(typeof(RequestDto));

			var matchingPath = controller.GetRestPathForRequest("GET", "/hello");
			Assert.That(matchingPath, Is.Not.Null);
			Assert.That(matchingPath.RequestType, Is.EqualTo(typeof(RequestDto)));
            Assert.That(matchingPath.IsOneWay, Is.EqualTo(false));
		}

		[Test]
		public void Does_notice_http_method()
		{
			IRestController controller = new RestController();
			controller.RegisterRestPath(new RestPath(typeof(RequestDto), "/hello", "GET", null, true));

			var matchingGetPath = controller.GetRestPathForRequest("GET", "/hello");
			Assert.That(matchingGetPath, Is.Not.Null);
            Assert.That(matchingGetPath.IsOneWay, Is.EqualTo(true));

			var matchingPostPath = controller.GetRestPathForRequest("POST", "/hello");
			Assert.That(matchingPostPath, Is.Null);
		}

		[Test]
		public void Does_notice_multiple_http_methods()
		{
			IRestController controller = new RestController();
			controller.RegisterRestPath(new RestPath(typeof(RequestDto), "/hello", "GET, POST", null, true));

			var matchingGetPath = controller.GetRestPathForRequest("GET", "/hello");
			Assert.That(matchingGetPath, Is.Not.Null);
            Assert.That(matchingGetPath.IsOneWay, Is.EqualTo(true));

			var matchingPostPath = controller.GetRestPathForRequest("POST", "/hello");
			Assert.That(matchingPostPath, Is.Not.Null);
            Assert.That(matchingPostPath.IsOneWay, Is.EqualTo(true));

			var matchingPutPath = controller.GetRestPathForRequest("PUT", "/hello");
			Assert.That(matchingPutPath, Is.Null);
		}

		[Test]
		public void Can_handle_similar_paths()
		{
			IRestController controller = new RestController();
			controller.RegisterRestPath(new RestPath(typeof(RequestDto), "/hello/{name}"));
			controller.RegisterRestPath(new RestPath(typeof(RequestDto), "/hello/{name}/catalogs"));

			var matchingPath = controller.GetRestPathForRequest("GET", "/hello/Demis") as RestPath;
			Assert.That(matchingPath.Path, Is.EqualTo("/hello/{name}"));

			var matchingPath2 = controller.GetRestPathForRequest("GET", "/hello/Steffen/catalogs") as RestPath;
			Assert.That(matchingPath2.Path, Is.EqualTo("/hello/{name}/catalogs"));
		}

		[Test]
		public void Can_handle_wildcard()
		{
			IRestController controller = new RestController();
			controller.RegisterRestPath(new RestPath(typeof(RequestDto), "/hello/{Name*}"));

			var matchingPath = controller.GetRestPathForRequest("GET", "/hello/Demis/and/Steffen/and/the/world");
			Assert.That(matchingPath, Is.Not.Null);
		}

        [Route("/hello")]
        class Hello
        {
        }

        [Test]
        public void Does_notice_Route_attributes()
        {
            IRestController controller = new RestController();
            controller.RegisterRestPaths(typeof(Hello));

            var matchingGetPath = controller.GetRestPathForRequest("GET", "/hello");
            Assert.That(matchingGetPath, Is.Not.Null);
            Assert.That(matchingGetPath.IsOneWay, Is.EqualTo(false));

            var matchingPostPath = controller.GetRestPathForRequest("POST", "/hello");
            Assert.That(matchingPostPath, Is.Not.Null);
            Assert.That(matchingPostPath.IsOneWay, Is.EqualTo(false));

            var matchingPutPath = controller.GetRestPathForRequest("PUT", "/hello");
            Assert.That(matchingPutPath, Is.Not.Null);
            Assert.That(matchingPutPath.IsOneWay, Is.EqualTo(false));

            var matchingDeletePath = controller.GetRestPathForRequest("DELETE", "/hello");
            Assert.That(matchingDeletePath, Is.Not.Null);
            Assert.That(matchingDeletePath.IsOneWay, Is.EqualTo(false)); 
        }

        [Route("/queue", IsOneWay = true)]
        class Queue
        {
        }

        [Test]
        public void Does_notice_different_call_style()
        {
            IRestController controller = new RestController();
            controller.RegisterRestPaths(typeof(Queue));

            var matchingGetPath = controller.GetRestPathForRequest("GET", "/queue");
            Assert.That(matchingGetPath, Is.Not.Null);
            Assert.That(matchingGetPath.IsOneWay, Is.EqualTo(true)); 
        }

        [Route("/test", Verbs = "GET")]
        class Test
        {
        }

        [Test]
        public void Does_notice_verbs_on_Route_attribute()
        {
            IRestController controller = new RestController();
            controller.RegisterRestPaths(typeof(Test));

            var matchingGetPath = controller.GetRestPathForRequest("GET", "/test");
            Assert.That(matchingGetPath, Is.Not.Null);
            Assert.That(matchingGetPath.IsOneWay, Is.EqualTo(false));

            var matchingPostPath = controller.GetRestPathForRequest("POST", "/test");
            Assert.That(matchingPostPath, Is.Null);
        }
	}
}
