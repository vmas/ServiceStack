using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.ServiceHost;

namespace ServiceStack.Tests.ServiceHost
{
	[TestFixture]
	public class ServiceControllerTests
	{
        class RequestDto { }
        class ResponseDto { }
        class Service1 : IRestGetService<RequestDto>
        {
            public object Get(RequestDto request)
            {
                return new ResponseDto();
            }
        }

        [Test]
        public void Can_register_service_which_does_not_implement_IService()
        {
            var factoryMock = new Mock<ITypeFactory>();
            factoryMock.Setup(x => x.CreateInstance(typeof(Service1))).Returns(new Service1());

            IServiceController controller = new ServiceController();
            controller.RegisterService(factoryMock.Object, typeof(Service1));

            var dto = new RequestDto();
            var result = controller.ExecuteAsync(dto, r => {}, new HttpRequestContext(dto, EndpointAttributes.HttpGet));
            Assert.That(result.Result, Is.TypeOf<ResponseDto>());
        }

        class RequestDto1 { }
        class RequestDto2 { }
        class RestService : IService<RequestDto1>, IRestGetService<RequestDto1>, IRestDeleteService<RequestDto2>
        {
            public object Get(RequestDto1 request)
            {
                return new ResponseDto();
            }

            public object Delete(RequestDto2 request)
            {
                return new ResponseDto();
            }

            public object Execute(RequestDto1 request)
            {
                return new ResponseDto();
            }
        }

        [Test]
        public void Can_register_service_with_multiple_request_DTOs()
        {
            var factoryMock = new Mock<ITypeFactory>();
            factoryMock.Setup(x => x.CreateInstance(typeof(RestService))).Returns(new RestService());

            IServiceController controller = new ServiceController();
            controller.RegisterService(factoryMock.Object, typeof(RestService));

            var dto = new RequestDto1();
            var result = controller.ExecuteAsync(dto, r => { }, new HttpRequestContext(dto, EndpointAttributes.HttpGet));
            Assert.That(result.Result, Is.TypeOf<ResponseDto>());

            var dto2 = new RequestDto2();
            var result2 = controller.ExecuteAsync(dto2, r => { }, new HttpRequestContext(dto2, EndpointAttributes.HttpDelete));
            Assert.That(result2.Result, Is.TypeOf<ResponseDto>());

            var result3 = controller.ExecuteAsync(dto, r => { });
            Assert.That(result3.Result, Is.TypeOf<ResponseDto>());
        }

        class RequestDto3 { }
        class RequestDto4 { }
        class GetService : IRestGetService<RequestDto3>, IRestGetService<RequestDto4>
        {
            public object Get(RequestDto3 request)
            {
                return new ResponseDto();
            }

            public object Get(RequestDto4 request)
            {
                return new ResponseDto();
            }
        }

        [Test]
        public void Can_chain_multiple_IRestGetService_interfaces_on_a_service()
        {
            var factoryMock = new Mock<ITypeFactory>();
            factoryMock.Setup(x => x.CreateInstance(typeof(GetService))).Returns(new GetService());

            IServiceController controller = new ServiceController();
            controller.RegisterService(factoryMock.Object, typeof(GetService));

            var dto = new RequestDto3();
            var result = controller.ExecuteAsync(dto, r => { }, new HttpRequestContext(dto, EndpointAttributes.HttpGet));
            Assert.That(result.Result, Is.TypeOf<ResponseDto>());

            var dto2 = new RequestDto4();
            var result2 = controller.ExecuteAsync(dto, r => { }, new HttpRequestContext(dto, EndpointAttributes.HttpGet));
            Assert.That(result2.Result, Is.TypeOf<ResponseDto>());
        }
	}
}
