using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class SyncServiceResult : IServiceResult
	{
		public object Result { get; set; }

		public object AsyncState
		{
			get { return null; }
		}

		public System.Threading.WaitHandle AsyncWaitHandle
		{
			get { return null; }
		}

		public bool CompletedSynchronously
		{
			get { return true; }
		}

		public bool IsCompleted
		{
			get { return true; }
		}
	}
}
