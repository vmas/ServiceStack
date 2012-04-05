using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.ServiceHost
{
	public class ServiceController
		: IServiceController
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceController));
		private const string ResponseDtoSuffix = "Response";

		public ServiceController()
		{
			this.RequestServiceTypeMap = new Dictionary<Type, Type>();
			this.ResponseServiceTypeMap = new Dictionary<Type, Type>();
			this.AllOperationTypes = new List<Type>();
			this.OperationTypes = new List<Type>();
			this.RequestTypeFactoryMap = new Dictionary<Type, Func<IHttpRequest, object>>();
			this.ResponseFactories = new List<Func<object, Action<IServiceResult>, IServiceResult>>();
			this.ServiceTypes = new HashSet<Type>();
		}

		readonly Dictionary<Type, Func<IRequestContext, object, object>> requestExecMap
			= new Dictionary<Type, Func<IRequestContext, object, object>>();

		readonly Dictionary<Type, ServiceAttribute> requestServiceAttrs
			= new Dictionary<Type, ServiceAttribute>();

		public Dictionary<Type, Type> ResponseServiceTypeMap { get; set; }

		public Dictionary<Type, Type> RequestServiceTypeMap { get; set; }

		public IList<Type> AllOperationTypes { get; protected set; }

		public IList<Type> OperationTypes { get; protected set; }

		public Dictionary<Type, Func<IHttpRequest, object>> RequestTypeFactoryMap { get; set; }

		public List<Func<object, Action<IServiceResult>, IServiceResult>> ResponseFactories { get; set; }

		public HashSet<Type> ServiceTypes { get; protected set; }

		public string DefaultOperationsNamespace { get; set; }

		public void RegisterService<TReq>(Func<IService<TReq>> serviceFactoryFn)
		{
			var requestType = typeof(TReq);
			Func<IRequestContext, object, object> handlerFn = (requestContext, dto) =>
			{
				var service = serviceFactoryFn();

				InjectRequestContext(service, requestContext);

				return ServiceExec<TReq>.Execute(
					service, (TReq)dto,
					requestContext != null ? requestContext.EndpointAttributes : EndpointAttributes.None);
			};

			this.RegisterService(requestType, typeof(IService<TReq>), handlerFn);
		}

		public void RegisterServices(ITypeFactory serviceFactory, IEnumerable<Type> serviceTypes)
		{
			foreach (var serviceType in serviceTypes)
				RegisterService(serviceFactory, serviceType);
		}

		public void RegisterService(ITypeFactory serviceFactory, Type serviceType)
		{
			if (serviceType.IsAbstract || serviceType.ContainsGenericParameters) return;

			foreach (var service in serviceType.GetInterfaces())
			{
				if (!service.IsGenericType
				    || service.GetGenericTypeDefinition() != typeof (IService<>)
				) continue;

				var requestType = service.GetGenericArguments()[0];
				RegisterService(requestType, serviceType, serviceFactory);
			}
		}

		public void RegisterService(Type requestType, Type serviceType, ITypeFactory serviceFactory)
		{
			var typeFactoryFn = CallServiceExecuteGeneric(requestType, serviceType);

			Func<IRequestContext, object, object> handlerFn = (requestContext, dto) =>
			{
				var service = serviceFactory.CreateInstance(serviceType);
                using (service as IDisposable) // using is happy if this expression evals to null
                {
                    InjectRequestContext(service, requestContext);

                    var endpointAttrs = requestContext != null
                        ? requestContext.EndpointAttributes
                        : EndpointAttributes.None;

                    try
                    {
                        //Executes the service and returns the result
                        var response = typeFactoryFn(dto, service, endpointAttrs);
						if (EndpointHost.AppHost != null) //tests
							EndpointHost.AppHost.Release(service);
                    	return response;
                    }
                    catch (TargetInvocationException tex)
                    {
                        //Mono invokes using reflection
                        throw tex.InnerException ?? tex;
                    }
                }
			};

			this.RegisterService(requestType, serviceType, handlerFn);
		}

		private void RegisterService(Type requestType, Type serviceType, Func<IRequestContext, object, object> handlerFn)
		{
			try
			{
				requestExecMap.Add(requestType, handlerFn);
			}
			catch (ArgumentException)
			{
				throw new AmbiguousMatchException(
					string.Format("Could not register the service '{0}' as another service with the definition of type 'IService<{1}>' already exists.",
					serviceType.FullName, requestType.Name));
			}

			EndpointHost.RestController.RegisterRestPaths(requestType);
			this.ServiceTypes.Add(serviceType);

			this.RequestServiceTypeMap[requestType] = serviceType;
			this.AllOperationTypes.Add(requestType);
			this.OperationTypes.Add(requestType);

			var responseTypeName = requestType.FullName + ResponseDtoSuffix;
			var responseType = AssemblyUtils.FindType(responseTypeName);
			if (responseType != null)
			{
				this.ResponseServiceTypeMap[responseType] = serviceType;
				this.AllOperationTypes.Add(responseType);
				this.OperationTypes.Add(responseType);
			}

			if (typeof(IRequiresRequestStream).IsAssignableFrom(requestType))
			{
				this.RequestTypeFactoryMap[requestType] = httpReq =>
				{
					var rawReq = (IRequiresRequestStream)requestType.CreateInstance();
					rawReq.RequestStream = httpReq.InputStream;
					return rawReq;
				};
			}
		}

		private static void InjectRequestContext(object service, IRequestContext requestContext)
		{
			if (requestContext == null) return;

			var serviceRequiresContext = service as IRequiresRequestContext;
			if (serviceRequiresContext != null)
			{
				serviceRequiresContext.RequestContext = requestContext;
			}

			var servicesRequiresHttpRequest = service as IRequiresHttpRequest;
			if (servicesRequiresHttpRequest != null)
				servicesRequiresHttpRequest.HttpRequest = requestContext.Get<IHttpRequest>();
		}

		private static Func<object, object, EndpointAttributes, object> CallServiceExecuteGeneric(
			Type requestType, Type serviceType)
		{
			try
			{
				var requestDtoParam = Expression.Parameter(typeof(object), "requestDto");
				var requestDtoStrong = Expression.Convert(requestDtoParam, requestType);

				var serviceParam = Expression.Parameter(typeof(object), "serviceObj");
				var serviceStrong = Expression.Convert(serviceParam, serviceType);

				var attrsParam = Expression.Parameter(typeof(EndpointAttributes), "attrs");

				var mi = ServiceExec.GetExecMethodInfo(serviceType, requestType);

				Expression callExecute = Expression.Call(
					mi, new Expression[] { serviceStrong, requestDtoStrong, attrsParam });

				var executeFunc = Expression.Lambda<Func<object, object, EndpointAttributes, object>>
					(callExecute, requestDtoParam, serviceParam, attrsParam).Compile();

				return executeFunc;

			}
			catch (Exception)
			{
				//problems with MONO, using reflection for temp fix
				return delegate(object request, object service, EndpointAttributes attrs)
				{
					var mi = ServiceExec.GetExecMethodInfo(serviceType, requestType);
					return mi.Invoke(null, new[] { service, request, attrs });
				};
			}
		}

		public object Execute(object request, IRequestContext requestContext = null)
		{
			var serviceResult = this.ExecuteAsync(request, r => { }, requestContext);
			return serviceResult.Result; //Blocking call
		}

		public IServiceResult ExecuteAsync(object request, Action<IServiceResult> callback, IRequestContext requestContext = null)
		{
			var requestType = request.GetType();
			var handlerFn = GetService(requestType);
			var responseDto = handlerFn(requestContext, request);

			if (responseDto != null)
			{
				foreach (var responseFactory in this.ResponseFactories)
				{
					var result = responseFactory(responseDto, callback);
					if (result != null)
						return result;
				}
			}

			var serviceResult = new SyncServiceResult(responseDto);
			callback(serviceResult);
			return serviceResult;
		}

		public Func<IRequestContext, object, object> GetService(Type requestType)
		{
			Func<IRequestContext, object, object> handlerFn;
			if (!requestExecMap.TryGetValue(requestType, out handlerFn))
			{
				throw new NotImplementedException(
						string.Format("Unable to resolve service '{0}'", requestType.Name));
			}

			return handlerFn;
		}
	}

}