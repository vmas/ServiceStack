using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.ServiceHost
{
	public interface IRestController
	{
		Dictionary<string, List<RestPath>> RestPathMap { get; }
        void RegisterDefaultPaths(IEnumerable<Type> requestTypes);
		void RegisterRestPaths(Type requestType);
		void RegisterRestPaths(IEnumerable<RestPath> restPaths);
		void RegisterRestPath(RestPath restPath);
		IRestPath GetRestPathForRequest(string httpMethod, string pathInfo);
	}
}
