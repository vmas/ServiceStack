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

namespace ServiceStack.WebHost.Endpoints.Handlers
{
	public class AsyncRestHandler : AsyncHandlerBase
	{
		public EndpointAttributes HandlerAttributes { get; set; }
		public IRestPath RestPath { get; set; }

		public AsyncRestHandler(IRestPath restPath)
		{
			this.RestPath = restPath;
			this.HandlerAttributes = EndpointAttributes.SyncReply;
		}

		private object GetRequestDto(IHttpRequest httpReq, IRestPath restPath)
		{
			var requestType = restPath.RequestType;
			using (Profiler.Current.Step("Deserialize Request"))
			{
				var requestParams = httpReq.GetRequestParams();
				return httpReq.GetRequestDto(
					restPath.RequestType, 
					httpReq.ContentType,
					dto => restPath.CreateRequest(httpReq.PathInfo, requestParams, dto) //Custom deserialization logic for REST endpoint
				);
			}
		}

		public override IServiceResult BeginProcessRequest(IHttpRequest req, IHttpResponse res, Action<IServiceResult> callback)
		{
			var responseContentType = EndpointHost.Config.DefaultContentType;
			try
			{
				if (!string.IsNullOrEmpty(req.ResponseContentType))
					responseContentType = req.ResponseContentType;
				EndpointHost.Config.AssertContentType(responseContentType);

				var requestDto = this.GetRequestDto(req, this.RestPath);
				if (EndpointHost.ApplyRequestFilters(req, res, requestDto)) return this.CancelRequestProcessing(callback);

				return this.GetServiceResult(req, res, requestDto, callback);
			}
			catch (Exception ex)
			{
				this.HandleException(req, res, ex);
				return this.CancelRequestProcessing(callback);
			}
		}

		public IServiceResult CancelRequestProcessing(Action<IServiceResult> callback)
		{
			var emptyServiceResult = new SyncServiceResult();
			callback(emptyServiceResult);
			return emptyServiceResult;
		}

		public IServiceResult GetServiceResult(IHttpRequest httpReq, IHttpResponse httpRes, object request, Action<IServiceResult> callback)
		{
			var requestContentType = ContentType.GetEndpointAttributes(httpReq.ResponseContentType);

			var endpointAttributes = HandlerAttributes | requestContentType | EndpointHandlerBase.GetEndpointAttributes(httpReq);
			var requestContext = new HttpRequestContext(httpReq, httpRes, request, endpointAttributes);
			return EndpointHost.ServiceManager.ServiceController.ExecuteAsync(request, callback, requestContext);
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
