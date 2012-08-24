using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;
using ServiceStack.Common.Web;
using System.Net;
using System.IO;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class ContentTypeTests
    {
        [Route("/request1", DefaultResponseContentType = ContentType.Json)]
        public class Request1 { }
        public class Request1Response
        {
            public string Content { get; set; }
        }

        public class Request1Service : IService<Request1>
        {
            public object Execute(Request1 request)
            {
                return new Request1Response { Content = "Hi!" };
            }
        }

        [Route("/request2", PreferredResponseContentType = ContentType.Json)]
        public class Request2 { }
        public class Request2Response
        {
            public string Content { get; set; }
        }

        public class Request2Service : IService<Request2>
        {
            public object Execute(Request2 request)
            {
                return new Request2Response { Content = "Hi!" };
            }
        }

        [Route("/request3")]
        public class Request3 { }
        public class Request3Response
        {
            public string Content { get; set; }
        }

        public class Request3Service : IService<Request3>
        {
            public object Execute(Request3 request)
            {
                return new Request3Response { Content = "Hi!" };
            }
        }

        [Route("/request4", AllowedContentTypes = ContentType.Json)]
        public class Request4
        {
            public string Content { get; set; }
        }

        public class Request4Response
        {
            public string Content { get; set; }
        }

        public class Request4Service : IService<Request4>
        {
            public object Execute(Request4 request)
            {
                return new Request4Response { Content = "Hi!" };
            }
        }

        const string ListeningOn = "http://localhost:8020/";

        class AppHost : AppHostHttpListenerBase
        {
            public AppHost()
                : base("Content type tests", typeof(Request1Service).Assembly)
            {
            }

            public override void Configure(Funq.Container container)
            {
                SetConfig(new EndpointHostConfig
                {
                    DefaultResponseContentType = ContentType.Json
                });
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

        private string ExecuteWebRequest(WebRequest webReq)
        {
            using (var webRes = webReq.GetResponse())
            using (var stream = webRes.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        [Test]
        public void Does_use_default_response_content_type_if_no_specified()
        {
            var webReq = (HttpWebRequest)WebRequest.Create(ListeningOn + "/request1");
            webReq.Method = "GET";
            webReq.Accept = null;
            webReq.ContentType = null;

            var response = ExecuteWebRequest(webReq);
            var deserialized = JsonSerializer.DeserializeFromString<Request1Response>(response);
            Assert.AreEqual("Hi!", deserialized.Content);
        }

        [Test]
        public void Does_notice_preferred_response_content_type()
        {
            var webReq = (HttpWebRequest)WebRequest.Create(ListeningOn + "/request2");
            webReq.Method = "GET";
            webReq.Accept = ContentType.Xml + "," + ContentType.Json + "," + ContentType.Html;
            webReq.ContentType = null;

            var response = ExecuteWebRequest(webReq);
            var deserialized = JsonSerializer.DeserializeFromString<Request1Response>(response);
            Assert.AreEqual("Hi!", deserialized.Content);
        }

        [Test]
        public void Does_use_global_default_response_content_type_if_no_other_information()
        {
            var webReq = (HttpWebRequest)WebRequest.Create(ListeningOn + "/request3");
            webReq.Method = "GET";
            webReq.Accept = null;
            webReq.ContentType = null;

            var response = ExecuteWebRequest(webReq);
            var deserialized = JsonSerializer.DeserializeFromString<Request1Response>(response);
            Assert.AreEqual("Hi!", deserialized.Content);
        }

        [Test]
        public void Notices_allowed_content_types()
        {
            var webReq = (HttpWebRequest)WebRequest.Create(ListeningOn + "/request4");
            webReq.Method = "POST";
            webReq.Accept = ContentType.Xml;
            webReq.ContentType = null;
            XmlSerializer.SerializeToStream(new Request4 { Content = "Bye!" }, webReq.GetRequestStream());

            var ex = Assert.Throws<WebException>(() => ExecuteWebRequest(webReq));
            ex.HasStatus(HttpStatusCode.BadRequest);
        }
    }
}
