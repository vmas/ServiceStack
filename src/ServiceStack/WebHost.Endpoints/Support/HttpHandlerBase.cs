using System;
using System.Runtime.Serialization;
using System.Web;
using ServiceStack.Text;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public abstract class HttpHandlerBase : IHttpHandler, IServiceStackHttpHandler
	{
		public void ProcessRequest(HttpContext context)
		{
            var operationName = context.Request.GetOperationName();
            var httpReq = new HttpRequestWrapper(operationName, context.Request);
            var httpRes = new HttpResponseWrapper(context.Response);
            ProcessRequest(httpReq, httpRes, operationName);
		}

		public bool IsReusable
		{
			get { return false; }
		}

        public abstract void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName);

        public static object DeserializeHttpRequest(Type operationType, IHttpRequest httpReq, string contentType)
        {
            var httpMethod = httpReq.HttpMethod;
            var queryString = httpReq.QueryString;

            if (httpMethod == HttpMethods.Get || httpMethod == HttpMethods.Delete || httpMethod == HttpMethods.Options)
            {
                try
                {
                    return KeyValueDataContractDeserializer.Instance.Parse(queryString, operationType);
                }
                catch (Exception ex)
                {
                    var msg = "Could not deserialize '{0}' request using KeyValueDataContractDeserializer: '{1}'.\nError: '{2}'"
                        .Fmt(operationType, queryString, ex);
                    throw new SerializationException(msg);
                }
            }

            var isFormData = httpReq.HasAnyOfContentTypes(ContentType.FormUrlEncoded, ContentType.MultiPartFormData);
            if (isFormData)
            {
                try
                {
                    return KeyValueDataContractDeserializer.Instance.Parse(httpReq.FormData, operationType);
                }
                catch (Exception ex)
                {
                    throw new SerializationException("Error deserializing FormData: " + httpReq.FormData, ex);
                }
            }

            var request = CreateContentTypeRequest(httpReq, operationType, contentType);
            return request;
        }

        protected static object CreateContentTypeRequest(IHttpRequest httpReq, Type requestType, string contentType)
        {
            try
            {
                if (!string.IsNullOrEmpty(contentType) && httpReq.ContentLength > 0)
                {
                    var deserializer = EndpointHost.AppHost.ContentTypeFilters.GetStreamDeserializer(contentType);
                    if (deserializer != null)
                    {
                        return deserializer(requestType, httpReq.InputStream);
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = "Could not deserialize '{0}' request using {1}'\nError: {2}"
                    .Fmt(contentType, requestType, ex);
                throw new SerializationException(msg);
            }
            return null;
        }
    }
}