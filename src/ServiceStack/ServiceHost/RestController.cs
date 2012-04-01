using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.ServiceHost
{
	public class RestController : IRestController
	{	
		private readonly Dictionary<string, List<RestPath>> restPathMap = new Dictionary<string, List<RestPath>>();
		public Dictionary<string, List<RestPath>> RestPathMap
		{
			get { return this.restPathMap; }
		}

		public void RegisterRestPaths(Type requestType)
		{
			var attrs = requestType.GetCustomAttributes(typeof(RestServiceAttribute), true);
			foreach (RestServiceAttribute attr in attrs)
			{
				var restPath = new RestPath(requestType, attr.Path, attr.Verbs, attr.DefaultContentType);
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
