using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.ServiceHost;
using System.Threading.Tasks;
using System.Reflection;

namespace ServiceStack.Plugins.Tasks
{
	public class TaskSupport : IPlugin
	{
		public void Register(IAppHost appHost)
		{
			EndpointHost.ServiceManager.ServiceController.ResponseConverters.Add(this.Convert);
		}

		private class TaskWrapper : IServiceResult
		{
			private Task task;
			private Func<object> getResultFn;

			public TaskWrapper(Task task, Func<object> getResultFn)
			{
				this.task = task;
				this.getResultFn = getResultFn;
			}

			public object Result
			{
				get { return getResultFn(); }
			}

			public object AsyncState
			{
				get { return task.AsyncState; }
			}

			public System.Threading.WaitHandle AsyncWaitHandle
			{
				get { return null; }
			}

			public bool CompletedSynchronously
			{
				get { return false; }
			}

			public bool IsCompleted
			{
				get { return task.IsCompleted; }
			}
		}

		public IServiceResult Convert(object service, object request, object response, Action<IServiceResult> callback)
		{
			var task = response as Task;
			if (task != null)
			{
				//TODO: Cache reflection logic
				var responseType = response.GetType();
				var resultProperty = responseType.GetProperty("Result");
				if (resultProperty != null)
				{
					Func<object> getResultFn = () => {
						try
						{
							return resultProperty.GetValue(response, new object[] { });
						}
						catch (TargetInvocationException ex)
						{
							var aggregateException = ex.InnerException;
							var serviceType = service.GetType();
							var handleExceptionFn = serviceType.GetMethod("HandleException");
							if(handleExceptionFn != null)
								return handleExceptionFn.Invoke(service, new object[] { request, aggregateException.InnerException });

							throw; //if no handle exception method exists re-throw the exception
						}
					};

					var taskWrapper = new TaskWrapper(task, getResultFn);

					//TODO: Add right synchronization context and improve exception handling
					task.ContinueWith(t => callback(taskWrapper));
					return taskWrapper;
				}
			}

			return null;
		}
	}
}
