using System;
using System.Collections.Generic;
using System.Reflection;

namespace ServiceStack.ServiceHost
{
	public class ServiceExec
	{
		private static readonly Dictionary<Type, MethodInfo> ServiceExecCache = new Dictionary<Type, MethodInfo>();

		public static MethodInfo GetExecMethodInfo(Type serviceType, Type requestType)
		{
			MethodInfo mi;
			lock (ServiceExecCache)
			{
				if (!ServiceExecCache.TryGetValue(requestType /*serviceType */, out mi))
				{
					var genericType = typeof(ServiceExec<>).MakeGenericType(requestType);
					
					mi = genericType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);

					ServiceExecCache.Add(requestType /* serviceType */, mi);
				}
			}

			return mi;
		}
	}

	public class ServiceExec<TReq>
	{
		public static object Execute(object service, TReq request, EndpointAttributes attrs)
		{
            if ((attrs & EndpointAttributes.HttpGet) == EndpointAttributes.HttpGet)
			{
				var restService = service as IRestGetService<TReq>;
				if (restService != null) return restService.Get(request);
			}
			else if ((attrs & EndpointAttributes.HttpPost) == EndpointAttributes.HttpPost)
			{
				var restService = service as IRestPostService<TReq>;
				if (restService != null) return restService.Post(request);
			}
			else if ((attrs & EndpointAttributes.HttpPut) == EndpointAttributes.HttpPut)
			{
				var restService = service as IRestPutService<TReq>;
				if (restService != null) return restService.Put(request);
			}
			else if ((attrs & EndpointAttributes.HttpDelete) == EndpointAttributes.HttpDelete)
			{
				var restService = service as IRestDeleteService<TReq>;
				if (restService != null) return restService.Delete(request);
			}
			else if ((attrs & EndpointAttributes.HttpPatch) == EndpointAttributes.HttpPatch)
			{
				var restService = service as IRestPatchService<TReq>;
				if (restService != null) return restService.Patch(request);
			}

            var normalService = service as IService<TReq>;
            if(normalService != null)
			    return normalService.Execute(request);

            throw new NotImplementedException(); //If we end up here, the requested method does not exist
		}
	}
}