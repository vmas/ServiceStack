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
			using (Profiler.Current.Step("Deserialize Request"))
			{
				var requestParams = httpReq.GetRequestParams();
				var requestDto = httpReq.GetRequestDto(restPath.RequestType, httpReq.ContentType);
				return restPath.CreateRequest(httpReq.PathInfo, requestParams, requestDto);
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

				var responseDto = this.GetResponseDto(req, res, requestDto);
				if (EndpointHost.ApplyResponseFilters(req, res, responseDto)) return this.CancelRequestProcessing(callback);

				var serviceResult = new SyncServiceResult() { Result = responseDto };
				callback(serviceResult);
				return serviceResult;
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

		public object GetResponseDto(IHttpRequest httpReq, IHttpResponse httpRes, object request)
		{
			var requestContentType = ContentType.GetEndpointAttributes(httpReq.ResponseContentType);

			var endpointAttributes = HandlerAttributes | requestContentType | EndpointHandlerBase.GetEndpointAttributes(httpReq);
			return EndpointHost.ExecuteService(request, endpointAttributes, httpReq, httpRes);
		}

		public override void EndProcessRequest(IHttpRequest req, IHttpResponse res, IServiceResult result)
		{
			try
			{
				if (res.IsClosed)
					return;

				var callback = req.GetJsonpCallback();
				var doJsonp = EndpointHost.Config.AllowJsonpRequests
							  && !string.IsNullOrEmpty(callback);

				if (doJsonp)
					res.WriteToResponse(req, result.Result, (callback + "(").ToUtf8Bytes(), ")".ToUtf8Bytes());
				else
					res.WriteToResponse(req, result.Result);
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
