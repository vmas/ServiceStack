using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;
using ServiceStack.MiniProfiler;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.Common.Web;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.Text;
using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints.Handlers
{
	public class AsyncRestHandler : AsyncHandlerBase
	{
		public IRestPath RestPath { get; set; }

		public AsyncRestHandler(IRestPath restPath)
		{
			this.RestPath = restPath;
		}

		private object GetRequestDto(IHttpRequest httpReq, IRestPath restPath)
		{
			var requestType = restPath.RequestType;

            string contentType = null;

            if (!string.IsNullOrEmpty(this.RestPath.AllowedContentTypes)) //If AllowedContentTypes is not empty, only specific content types are allowed for this request
            {
                var validContentTypes = this.RestPath.AllowedContentTypes.Split(',');

                if (!string.IsNullOrEmpty(httpReq.ContentType))
                    contentType = validContentTypes.FirstOrDefault(x => httpReq.ContentType.StartsWith(x)); //Select a matching, allowed content type

                //If no allowed content type was given from the client, the first allowed one is taken
                if (contentType == null)
                    contentType = validContentTypes.First();         
            }
            else
            {
                contentType = httpReq.ContentType;
            }

			using (Profiler.Current.Step("Deserialize Request"))
			{
				var requestParams = httpReq.GetRequestParams();
				return httpReq.GetRequestDto(
					restPath.RequestType, 
					contentType,
					dto => restPath.CreateRequest(httpReq.PathInfo, requestParams, dto) //Custom deserialization logic for REST endpoint
				);
			}
		}

        private void SetResponseContentType(IHttpRequest req)
        {
            if (req.AcceptTypes == null || req.AcceptTypes.Count() <= 0)
            {
                if (!string.IsNullOrEmpty(this.RestPath.DefaultResponseContentType))
                    req.ResponseContentType = this.RestPath.DefaultResponseContentType;
            }
            else if (!string.IsNullOrEmpty(this.RestPath.PreferredResponseContentType) &&
                req.AcceptTypes.Contains(this.RestPath.PreferredResponseContentType))
            {
                req.ResponseContentType = this.RestPath.PreferredResponseContentType;
            }
        }

        //Used by ASP.net
        public override IAsyncResult BeginProcessRequest(System.Web.HttpContext context, AsyncCallback cb, object extraData)
        {
            var operationName = this.RestPath.RequestType.Name;
            this.HttpRequest = new HttpRequestWrapper(operationName, context.Request);
            this.HttpResponse = new HttpResponseWrapper(context.Response);
            Action<IServiceResult> callback = result => cb(result);

            return this.BeginProcessRequest(this.HttpRequest, this.HttpResponse, callback);
        }

		public override IServiceResult BeginProcessRequest(IHttpRequest req, IHttpResponse res, Action<IServiceResult> callback)
		{
			try
			{
                this.SetResponseContentType(req);
                EndpointHost.Config.AssertContentType(req.ResponseContentType);

                var requestDto = this.GetRequestDto(req, this.RestPath);

                var endpointAttributes = GetEndpointAttributes(req);
                var requestContext = new HttpRequestContext(req, res, requestDto, endpointAttributes);

				if (EndpointHost.ApplyRequestFilters(req, res, requestDto)) return this.CancelRequestProcessing(callback);

                if (this.RestPath.IsOneWay)
                {
                    EndpointHost.ServiceManager.ServiceController.ExecuteOneWay(requestDto, requestContext);
                    return this.CancelRequestProcessing(callback);
                }
                else
                {
                    return EndpointHost.ServiceManager.ServiceController.ExecuteAsync(requestDto, callback, requestContext);
                }
			}
			catch (Exception ex)
			{
				this.HandleException(req, res, ex);
				return this.CancelRequestProcessing(callback);
			}
		}

		public override void EndProcessRequest(IHttpRequest req, IHttpResponse res, IServiceResult result)
		{
			try
			{
				if (res.IsClosed)
					return;

				var responseDto = result.Result;
				if (EndpointHost.ApplyResponseFilters(req, res, responseDto)) return;

				var callback = req.GetJsonpCallback();
				var doJsonp = EndpointHost.Config.AllowJsonpRequests
							  && !string.IsNullOrEmpty(callback);

                if (doJsonp && !(responseDto is CompressedResult))
					res.WriteToResponse(req, responseDto, (callback + "(").ToUtf8Bytes(), ")".ToUtf8Bytes());
				else
					res.WriteToResponse(req, responseDto);
			}
			catch (Exception ex)
			{
				this.HandleException(req, res, ex);
			}
			finally
			{
				if (!res.IsClosed)
					res.Close();
			}
		}
	}
}
