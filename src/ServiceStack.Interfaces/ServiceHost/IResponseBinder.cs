using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.ServiceHost
{
	public interface IResponseBinder
	{
		IServiceResult Convert(IHttpRequest httpReq, IHttpResponse httpRes, object responseDto, Action<IServiceResult> callback);
	}
}
