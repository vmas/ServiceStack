using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public interface IServiceStackHttpAsyncHandler
	{
		IServiceResult BeginProcessRequest(IHttpRequest req, IHttpResponse res, Action<IServiceResult> callback);
		void EndProcessRequest(IHttpRequest req, IHttpResponse res, IServiceResult result);
	}
}
