using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Common.Web;

namespace ServiceStack.ServiceHost
{
	public class RestController : IRestController
	{	
		private readonly Dictionary<string, List<RestPath>> restPathMap = new Dictionary<string, List<RestPath>>();
		public Dictionary<string, List<RestPath>> RestPathMap
		{
			get { return this.restPathMap; }
		}

        public void RegisterDefaultPaths(IEnumerable<Type> requestTypes)
        {
            foreach (var requestType in requestTypes)
            {
                this.RegisterRestPath(new RestPath(requestType, "/xml/requestreply/" + requestType.Name, "*", ContentType.Xml, ContentType.Xml, false));
                this.RegisterRestPath(new RestPath(requestType, "/xml/oneway/" + requestType.Name, "*", ContentType.Xml, ContentType.Xml, true));

                this.RegisterRestPath(new RestPath(requestType, "/json/requestreply/" + requestType.Name, "*", ContentType.Json, ContentType.Json, false));
                this.RegisterRestPath(new RestPath(requestType, "/json/oneway/" + requestType.Name, "*", ContentType.Json, ContentType.Json, true));

                this.RegisterRestPath(new RestPath(requestType, "/html/requestreply/" + requestType.Name, "*", ContentType.Html, ContentType.Html, false));
                this.RegisterRestPath(new RestPath(requestType, "/html/oneway/" + requestType.Name, "*", ContentType.Html, ContentType.Html, true));

                this.RegisterRestPath(new RestPath(requestType, "/jsv/requestreply/" + requestType.Name, "*", ContentType.Jsv, ContentType.Jsv, false));
                this.RegisterRestPath(new RestPath(requestType, "/jsv/oneway/" + requestType.Name, "*", ContentType.Jsv, ContentType.Jsv, true));

                this.RegisterRestPath(new RestPath(requestType, "/csv/requestreply/" + requestType.Name, "*", ContentType.Csv, ContentType.Csv, false));
                this.RegisterRestPath(new RestPath(requestType, "/csv/oneway/" + requestType.Name, "*", ContentType.Csv, ContentType.Csv, true));

                var formContentType = ContentType.FormUrlEncoded + ',' + ContentType.MultiPartFormData;
                this.RegisterRestPath(new RestPath(requestType, "/form/requestreply/" + requestType.Name, "*", null, formContentType, false));
                this.RegisterRestPath(new RestPath(requestType, "/form/oneway/" + requestType.Name, "*", null, formContentType, true));
            }
        }

		public void RegisterRestPaths(Type requestType)
		{
			var attrs = requestType.GetCustomAttributes(typeof(RouteAttribute), true);
			foreach (RouteAttribute attr in attrs)
			{
				var restPath = new RestPath(requestType, attr.Path, attr.Verbs, attr.DefaultContentType, attr.AllowedContentTypes, attr.IsOneWay);
				if (!restPath.IsValid)
					throw new NotSupportedException(string.Format(
						"RestPath '{0}' on Type '{1}' is not Valid", attr.Path, requestType.Name));

				RegisterRestPath(restPath);
			}
		}

		public void RegisterRestPaths(IEnumerable<RestPath> restPaths)
		{
			foreach (var restPath in restPaths)
				this.RegisterRestPath(restPath);
		}

		public void RegisterRestPath(RestPath restPath)
		{
			List<RestPath> pathsAtFirstMatch;
			if (!RestPathMap.TryGetValue(restPath.FirstMatchHashKey, out pathsAtFirstMatch))
			{
				pathsAtFirstMatch = new List<RestPath>();
				RestPathMap[restPath.FirstMatchHashKey] = pathsAtFirstMatch;
			}
			pathsAtFirstMatch.Add(restPath);
		}

		public IRestPath GetRestPathForRequest(string httpMethod, string pathInfo)
		{
			var matchUsingPathParts = RestPath.GetPathPartsForMatching(pathInfo);

			List<RestPath> firstMatches;

			var yieldedHashMatches = RestPath.GetFirstMatchHashKeys(matchUsingPathParts);
			foreach (var potentialHashMatch in yieldedHashMatches)
			{
				if (!this.RestPathMap.TryGetValue(potentialHashMatch, out firstMatches)) continue;

				foreach (var restPath in firstMatches)
				{
					if (restPath.IsMatch(httpMethod, matchUsingPathParts)) return restPath;
				}
			}

			var yieldedWildcardMatches = RestPath.GetFirstMatchWildCardHashKeys(matchUsingPathParts);
			foreach (var potentialHashMatch in yieldedWildcardMatches)
			{
				if (!this.RestPathMap.TryGetValue(potentialHashMatch, out firstMatches)) continue;

				foreach (var restPath in firstMatches)
				{
					if (restPath.IsMatch(httpMethod, matchUsingPathParts)) return restPath;
				}
			}

			return null;
		}
	}
}
