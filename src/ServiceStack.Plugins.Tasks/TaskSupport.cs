using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.ServiceHost;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace ServiceStack.Plugins.Tasks
{
	public delegate object HandleTaskException(object service, object request, Exception ex);
	public delegate object GetTaskResult(object service, object request, object response);

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

		private static ConcurrentDictionary<Tuple<Type, Type, Type>, GetTaskResult> converterCache 
			= new ConcurrentDictionary<Tuple<Type, Type, Type>, GetTaskResult>();

		public IServiceResult Convert(object service, object request, object response, Action<IServiceResult> callback)
		{
			var responseType = response.GetType();
			if (responseType.GetGenericTypeDefinition() == typeof(Task<>))
			{
				var serviceType = service.GetType();
				var requestType = request.GetType();
				var task = response as Task;

				var key = new Tuple<Type, Type, Type>(serviceType, requestType, responseType);
				GetTaskResult getResultFn = null;
				if (!converterCache.TryGetValue(key, out getResultFn))
				{
					getResultFn = this.GenerateGetResultFn(serviceType, requestType, responseType);
					converterCache.TryAdd(key, getResultFn);
				}

				var taskWrapper = new TaskWrapper(task, () => getResultFn(service, request, response));

				task.ContinueWith(t => callback(taskWrapper));
				return taskWrapper;
			}

			return null;
		}

		private GetTaskResult GenerateGetResultFn(Type serviceType, Type requestType, Type responseType)
		{
			var responseParameter = Expression.Parameter(typeof(object), "response");
			var resultProperty = responseType.GetProperty("Result");

			var getResultFn = Expression.Lambda<Func<object, object>>(
				Expression.Property(Expression.Convert(responseParameter, responseType), resultProperty), 
				responseParameter
			).Compile();

			var serviceParameter = Expression.Parameter(typeof(object), "service");
			var requestParameter = Expression.Parameter(typeof(object), "request");
			var exceptionParameter = Expression.Parameter(typeof(Exception), "ex");

			var handleExceptionMethod = serviceType.GetMethod("HandleException");
			if (handleExceptionMethod == null) //Don't add exception handling logic if HandleException() doesn't exist
				return (service, request, response) => getResultFn(response);

			var handleExceptionFn = Expression.Lambda<HandleTaskException>(
				Expression.Call(
					Expression.Convert(serviceParameter, serviceType), 
					handleExceptionMethod, 
					Expression.Convert(requestParameter, requestType), 
					exceptionParameter
				),
				serviceParameter, requestParameter, exceptionParameter
			).Compile();

			GetTaskResult tryGetResultFn = (service, request, response) =>
			{
				try
				{
					return getResultFn(response);
				}
				catch (AggregateException ex)
				{
					return handleExceptionFn(service, request, ex.InnerException);
				}
			};

			return tryGetResultFn;
		}
	}
}
