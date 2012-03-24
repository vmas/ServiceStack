using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Plugins.Tasks
{
	public class TaskSupport : IPlugin
	{
		public void Register(IAppHost appHost)
		{
			EndpointHost.ResponseBinders.Add(new TaskConverter());
		}
	}
}
