using System;
using System.Collections.Generic;
using ServiceStack.Configuration;

namespace ServiceStack.ServiceHost
{
    /// <summary>
    /// Responsible for executing the operation within the specified context.
    /// </summary>
    /// <value>The operation types.</value>
    public interface IServiceController
    {
        Dictionary<Type, Type> RequestServiceTypeMap { get; set; }
        Dictionary<Type, Type> ResponseServiceTypeMap { get; set; }

        Dictionary<Type, Func<IHttpRequest, object>> RequestTypeFactoryMap { get; set; }

        List<Func<object, object, object, Action<IServiceResult>, IServiceResult>> ResponseConverters { get; set; }

        HashSet<Type> ServiceTypes { get; }

        /// <summary>
        /// Returns a list of operation types (ie request & response types) available in this service
        /// </summary>
        /// <value>The operation types.</value>
        IList<Type> OperationTypes { get; }

        /// <summary>
        /// Returns a list of all request types
        /// </summary>
        IList<Type> RequestTypes { get; }

        /// <summary>
        /// Executes the DTO request under the supplied requestContext.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        [Obsolete]
        object Execute(object request, IRequestContext requestContext = null);

        IServiceResult ExecuteAsync(object request, Action<IServiceResult> callback, IRequestContext requestContext = null);

        void ExecuteOneWay(object request, IRequestContext requestContext = null);

        void RegisterServices(ITypeFactory serviceFactory, IEnumerable<Type> serviceTypes);
        void RegisterService(ITypeFactory serviceFactory, Type serviceType);

        Func<IRequestContext, object, Action<IServiceResult>, IServiceResult> GetService(Type requestType);
    }
}