using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceClient.Web;
using System.Net;
using System.IO;
using ServiceStack.Common.Web;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class Default
    {
        public string Name { get; set; }
        public bool ThrowException { get; set; }
    }

    public class DefaultResponse
    {
        public string Name { get; set; }
    }

    public class DefaultService : ServiceBase<Default>
    {
        public static string name;
        public static int timesOneWayCalled = 0;

        protected override object Run(Default request)
        {
            return new DefaultResponse() { Name = request.Name };
        }

        public override void ExecuteOneWay(Default request)
        {
            timesOneWayCalled++;
            name = request.Name;

            if (request.ThrowException)
                throw new NotSupportedException();
        }
    }

    [TestFixture]
    public class DefaultRouteTests
    {
        const string ListeningOn = "http://localhost:8020/";

        class DefaultAppHost : AppHostHttpListenerBase
        {
            public DefaultAppHost()
                : base("Default services", typeof(DefaultService).Assembly)
            {
            }

            public override void Configure(Funq.Container container)
            {
            }
        }

        DefaultAppHost appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new DefaultAppHost();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        private string ExecuteWebRequest(WebRequest webReq)
        {
            using (var webRes = webReq.GetResponse())
            using (var stream = webRes.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        [TestCase("/xml/oneway/default")]
        [TestCase("/json/oneway/default")]
        [TestCase("/csv/oneway/default")]
        [TestCase("/jsv/oneway/default")]
        public void Can_call_execute_one_way_route(string url)
        {
            DefaultService.timesOneWayCalled = 0;
            DefaultService.name = null;

            var webReq = (HttpWebRequest)WebRequest.Create(ListeningOn + url);

            var response = this.ExecuteWebRequest(webReq);

            Assert.That(DefaultService.timesOneWayCalled, Is.EqualTo(1));
            Assert.That(DefaultService.name, Is.Null);
            Assert.That(response, Is.EqualTo(""));
        }

        [TestCase("/json/oneway/default", "{\"ThrowException\":true}")]
        [TestCase("/jsv/oneway/default", "{ThrowException:true}")]
        public void Handles_exception_on_one_way_route(string url, string content)
        {
            DefaultService.timesOneWayCalled = 0;
            DefaultService.name = null;

            var webReq = (HttpWebRequest)WebRequest.Create(ListeningOn + url);
            webReq.Method = HttpMethods.Post;

            var requestContent = Encoding.UTF8.GetBytes(content);
            using (var req = webReq.GetRequestStream())
                req.Write(requestContent, 0, requestContent.Length);

            var ex = Assert.Throws<WebException>(() => this.ExecuteWebRequest(webReq));
            var response = (HttpWebResponse)ex.Response;

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(DefaultService.timesOneWayCalled, Is.EqualTo(1));
        }

        [Test]
        public void Ignores_format_query_string_for_deserialization_but_not_for_serialization()
        {
            DefaultService.timesOneWayCalled = 0;
            DefaultService.name = "";

            var webReq = (HttpWebRequest)WebRequest.Create(ListeningOn + "/json/requestreply/default?format=xml");
            webReq.Method = HttpMethods.Post;

            var json = Encoding.UTF8.GetBytes("{\"Name\":\"Bumba\"}"); //Default routes always use the format specified in the URL ('/json/') to serialize the request
            using (var req = webReq.GetRequestStream())
                req.Write(json, 0, json.Length);

            var responseString = this.ExecuteWebRequest(webReq);
            var response = XmlSerializer.DeserializeFromString<DefaultResponse>(responseString); //Default routes use the query string / 'Accept' header to determine the response format

            Assert.That(response.Name, Is.EqualTo("Bumba"));
        }

        [Test]
        public void Ignores_headers_for_deserialization_but_not_for_serialization()
        {
            DefaultService.timesOneWayCalled = 0;
            DefaultService.name = "";

            var webReq = (HttpWebRequest)WebRequest.Create(ListeningOn + "/json/requestreply/default");
            webReq.ContentType = ContentType.Xml;
            webReq.Accept = ContentType.Xml;
            webReq.Method = HttpMethods.Post;

            var json = Encoding.UTF8.GetBytes("{\"Name\":\"Bumba\"}"); //Default routes always use the format specified in the URL ('/json/') to serialize the request
            using (var req = webReq.GetRequestStream())
                req.Write(json, 0, json.Length);

            var responseString = this.ExecuteWebRequest(webReq);
            var response = XmlSerializer.DeserializeFromString<DefaultResponse>(responseString); //Default routes use the query string / 'Accept' header to determine the response format

            Assert.That(response.Name, Is.EqualTo("Bumba"));
        }

        [Test]
        public void Throws_exception_if_wrong_format()
        {
            DefaultService.timesOneWayCalled = 0;
            DefaultService.name = "";

            var webReq = (HttpWebRequest)WebRequest.Create(ListeningOn + "/json/requestreply/default?format=xml");
            webReq.ContentType = ContentType.Xml; //Default routes ignore content type, they use the format specified in the url ('/json/') to deserialize the request
            webReq.Accept = ContentType.Xml;
            webReq.Method = HttpMethods.Post;

            var request = new Default() { Name = "Bumba" };
            var json = Encoding.UTF8.GetBytes(XmlSerializer.SerializeToString(request));
            using (var req = webReq.GetRequestStream())
                req.Write(json, 0, json.Length);

            var ex = Assert.Throws<WebException>(() => this.ExecuteWebRequest(webReq));
            var response = (HttpWebResponse)ex.Response;

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
    }
}
