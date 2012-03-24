using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.ServiceHost
{
	public interface IServiceResult : IAsyncResult
	{
		object Result { get; }
	}
}
