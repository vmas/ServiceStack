using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;
using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Logging;

namespace ServiceStack.WebHost.Endpoints.Handlers
{
	public abstract class AsyncHandlerBase : IHttpAsyncHandler, IServiceStackHttpAsyncHandler
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(AsyncHandlerBase));

		public IHttpRequest HttpRequest { get; set; }
		public IHttpResponse HttpResponse { get; set; }

		public virtual IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
		{
			var operationName = context.Request.GetOperationName();
			this.HttpRequest = new HttpRequestWrapper(operationName, context.Request);
			this.HttpResponse = new HttpResponseWrapper(context.Response);
			Action<IServiceResult> callback = result => cb(result);

			return this.BeginProcessRequest(this.HttpRequest, this.HttpResponse, callback);
		}

		public abstract IServiceResult BeginProcessRequest(IHttpRequest req, IHttpResponse res, Action<IServiceResult> callback);

		public void EndProcessRequest(IAsyncResult result)
		{
			var serviceResult = result as IServiceResult;
			this.EndProcessRequest(this.HttpRequest, this.HttpResponse, serviceResult);
		}

		public abstract void EndProcessRequest(IHttpRequest req, IHttpResponse res, IServiceResult result);

		public bool IsReusable
		{
			get { return false; }
		}

		protected virtual void HandleException(IHttpRequest httpReq, IHttpResponse httpRes, Exception ex)
		{
			var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
			Log.Error(errorMessage, ex);

			try
			{
				var statusCode = ex is SerializationException ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError;
				//httpRes.WriteToResponse always calls .Close in it's finally statement so 
				//if there is a problem writing to response, by now it will be closed
				if (!httpRes.IsClosed)
				{
                    httpRes.WriteErrorToResponse(httpReq.ResponseContentType, httpReq.OperationName, errorMessage, ex, statusCode);
				}
			}
			catch (Exception writeErrorEx)
			{
				//Exception in writing to response should not hide the original exception
				Log.Info("Failed to write error to response: {0}", writeErrorEx);

				//rethrow the original exception
				throw ex;
			}
		}

        protected IServiceResult CancelRequestProcessing(Action<IServiceResult> callback)
        {
            var emptyServiceResult = new SyncServiceResult();
            callback(emptyServiceResult);
            return emptyServiceResult;
        }

		public void ProcessRequest(HttpContext context)
		{
			throw new NotSupportedException();
		}

        protected EndpointAttributes GetEndpointAttributes(IHttpRequest request)
        {
            var endpointAttributes = EndpointAttributes.None;

            endpointAttributes |= HttpMethods.GetEndpointAttribute(request.HttpMethod);
            endpointAttributes |= request.IsSecureConnection ? EndpointAttributes.Secure : EndpointAttributes.InSecure;
            endpointAttributes |= request.IsLocal ? EndpointAttributes.Localhost : EndpointAttributes.External;
            
            return endpointAttributes;
        }
	}
}
