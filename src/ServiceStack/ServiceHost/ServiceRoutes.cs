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
            RestPaths.Add(new RestPath(typeof(TRequest), restPath, verbs, null, null, null, false));
            return this;
        }

        public IServiceRoutes Add<TRequest>(string restPath, string verbs = null, string defaultResponseContentType = null,
            string preferredResponseContentType = null, string allowedContentTypes = null, bool isOneWay = false)
        {
            RestPaths.Add(new RestPath(typeof(TRequest), restPath, verbs, defaultResponseContentType,
                preferredResponseContentType, allowedContentTypes, isOneWay));
            return this;
        }

        public IServiceRoutes Add(Type requestType, string restPath, string verbs, string defaultContentType)
        {
            RestPaths.Add(new RestPath(requestType, restPath, verbs, defaultContentType, null, null, false));
            return this;
        }

    }
}