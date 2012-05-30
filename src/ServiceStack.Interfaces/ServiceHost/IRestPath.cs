using System;
using System.Collections.Generic;

namespace ServiceStack.ServiceHost
{
    public interface IRestPath
    {
        Type RequestType { get; }

        string DefaultContentType { get; }

        string AllowedContentTypes { get; }

        bool IsOneWay { get; }

        object CreateRequest(string pathInfo, Dictionary<string, string> queryStringAndFormData, object fromInstance);
    }
}