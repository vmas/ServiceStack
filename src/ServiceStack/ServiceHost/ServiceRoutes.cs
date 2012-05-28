using System;
using System.Collections.Generic;

namespace ServiceStack.ServiceHost
{
	public class ServiceRoutes : IServiceRoutes
	{
		public readonly List<RestPath> RestPaths = new List<RestPath>();

		public IServiceRoutes Add<TRequest>(string restPath)
		{
			RestPaths.Add(new RestPath(typeof(TRequest), restPath));
			return this;
		}

		public IServiceRoutes Add<TRequest>(string restPath, string verbs)
		{
			RestPaths.Add(new RestPath(typeof(TRequest), restPath, verbs, null, EndpointAttributes.SyncReply));
			return this;
		}

		public IServiceRoutes Add<TRequest>(string restPath, string verbs, string defaultContentType)
		{
			RestPaths.Add(new RestPath(typeof(TRequest), restPath, verbs, defaultContentType, EndpointAttributes.SyncReply));
			return this;
		}

        public IServiceRoutes Add<TRequest>(string restPath, string verbs, string defaultContentType, EndpointAttributes pathAttributes)
        {
            var allPathAttributes = EndpointAttributes.SyncReply;

            //Check if attributes contain call style
            if ((EndpointAttributes.AllCallStyles & pathAttributes) != 0)
            {
                allPathAttributes = pathAttributes;
            }
            else
            {
                allPathAttributes |= pathAttributes;
            }

            RestPaths.Add(new RestPath(typeof(TRequest), restPath, verbs, defaultContentType, allPathAttributes));
            return this;
        }

        public IServiceRoutes Add(Type requestType, string restPath, string verbs, string defaultContentType)
        {
            RestPaths.Add(new RestPath(requestType, restPath, verbs, defaultContentType, EndpointAttributes.SyncReply));
            return this;
        }
	}
}