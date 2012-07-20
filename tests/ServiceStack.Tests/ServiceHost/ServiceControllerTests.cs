using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.ServiceHost;

namespace ServiceStack.Tests.ServiceHost
{
	[TestFixture]
	public class ServiceControllerTests
	{
        class Factory : ITypeFactory
        {
            public object CreateInstance(Type type)
            {
                return new RestService();
            }
        }

        class RequestDto { }
        class RequestDto2 { }
        class ResponseDto { }
        class RestService : IService<RequestDto>, IRestGetService<RequestDto>, IRestDeleteService<RequestDto2>
        {
            public object Get(RequestDto request)
            {
                return new ResponseDto();
            }

            public object Delete(RequestDto2 request)
            {
                return new ResponseDto();
            }

            public object Execute(RequestDto request)
            {
                return new ResponseDto();
            }
        }

        [Test]
        public void Can_register_IRestService()
        {
            IServiceController controller = new ServiceController();
            controller.RegisterService(new Factory(), typeof(RestService));

            var dto = new RequestDto();
            var result = controller.ExecuteAsync(dto, r => { }, new HttpRequestContext(dto, EndpointAttributes.HttpGet));
            Assert.That(result.Result, Is.TypeOf<ResponseDto>());

            var dto2 = new RequestDto2();
            var result2 = controller.ExecuteAsync(dto2, r => { }, new HttpRequestContext(dto2, EndpointAttributes.HttpDelete));
            Assert.That(result.Result, Is.TypeOf<ResponseDto>());

            var result3 = controller.ExecuteAsync(dto, r => { });
            Assert.That(result.Result, Is.TypeOf<ResponseDto>());
        }
	}
}
