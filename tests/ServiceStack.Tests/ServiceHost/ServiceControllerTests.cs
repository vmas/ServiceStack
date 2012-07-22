using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Support;

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

            Assert.Throws<NotImplementedException>(() => controller.ExecuteAsync(dto, r => { }, new HttpRequestContext(dto, EndpointAttributes.HttpPut)));

            Assert.That(controller.OperationTypes.Count, Is.EqualTo(1));
            Assert.That(controller.OperationTypes, Contains.Item(typeof(RequestDto)));

            Assert.That(controller.ServiceTypes.Count, Is.EqualTo(1));
            Assert.That(controller.ServiceTypes, Contains.Item(typeof(Service1)));

            Assert.That(controller.RequestTypes.Count, Is.EqualTo(1));
            Assert.That(controller.RequestTypes, Contains.Item(typeof(RequestDto)));

            Assert.That(controller.RequestServiceTypeMap.Count, Is.EqualTo(1));
            Assert.AreEqual(typeof(Service1), controller.RequestServiceTypeMap[typeof(RequestDto)]);

            Assert.That(controller.ResponseServiceTypeMap, Is.Empty); //We don't follow the response DTO naming convention
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

            Assert.That(controller.OperationTypes.Count, Is.EqualTo(2));
            Assert.That(controller.OperationTypes, Contains.Item(typeof(RequestDto1)));
            Assert.That(controller.OperationTypes, Contains.Item(typeof(RequestDto2)));

            Assert.That(controller.ServiceTypes.Count, Is.EqualTo(1));
            Assert.That(controller.ServiceTypes, Contains.Item(typeof(RestService)));

            Assert.That(controller.RequestTypes.Count, Is.EqualTo(2));
            Assert.That(controller.RequestTypes, Contains.Item(typeof(RequestDto1)));
            Assert.That(controller.RequestTypes, Contains.Item(typeof(RequestDto2)));

            Assert.That(controller.RequestServiceTypeMap.Count, Is.EqualTo(2));
            Assert.AreEqual(typeof(RestService), controller.RequestServiceTypeMap[typeof(RequestDto1)]);
            Assert.AreEqual(typeof(RestService), controller.RequestServiceTypeMap[typeof(RequestDto2)]);

            Assert.That(controller.ResponseServiceTypeMap, Is.Empty); //We don't follow the response DTO naming convention
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

            Assert.That(controller.OperationTypes.Count, Is.EqualTo(2));
            Assert.That(controller.OperationTypes, Contains.Item(typeof(RequestDto3)));
            Assert.That(controller.OperationTypes, Contains.Item(typeof(RequestDto4)));

            Assert.That(controller.ServiceTypes.Count, Is.EqualTo(1));
            Assert.That(controller.ServiceTypes, Contains.Item(typeof(GetService)));

            Assert.That(controller.RequestTypes.Count, Is.EqualTo(2));
            Assert.That(controller.RequestTypes, Contains.Item(typeof(RequestDto3)));
            Assert.That(controller.RequestTypes, Contains.Item(typeof(RequestDto4)));

            Assert.That(controller.RequestServiceTypeMap.Count, Is.EqualTo(2));
            Assert.AreEqual(typeof(GetService), controller.RequestServiceTypeMap[typeof(RequestDto3)]);
            Assert.AreEqual(typeof(GetService), controller.RequestServiceTypeMap[typeof(RequestDto4)]);

            Assert.That(controller.ResponseServiceTypeMap, Is.Empty); //We don't follow the response DTO naming convention
        }

        class RequestDto5 { }
        class OneWayService : IService<RequestDto5>, IRestGetService<RequestDto5>, IOneWayService<RequestDto5>
        {
            public static bool oneWayWasCalled = false;

            public object Execute(RequestDto5 request)
            {
                return new ResponseDto();
            }

            public void ExecuteOneWay(RequestDto5 request)
            {
                oneWayWasCalled = true;
            }

            public object Get(RequestDto5 request)
            {
                return new ResponseDto();
            }
        }

        [Test]
        public void Can_combine_IOneWayService_with_IService()
        {
            OneWayService.oneWayWasCalled = false;

            var factoryMock = new Mock<ITypeFactory>();
            factoryMock.Setup(x => x.CreateInstance(typeof(OneWayService))).Returns(new OneWayService());

            IServiceController controller = new ServiceController();
            controller.RegisterService(factoryMock.Object, typeof(OneWayService));

            var dto = new RequestDto5();
            var result = controller.ExecuteAsync(dto, r => { }, new HttpRequestContext(dto, EndpointAttributes.HttpPost));
            Assert.That(result.Result, Is.TypeOf<ResponseDto>());

            var result2 = controller.ExecuteAsync(dto, r => { }, new HttpRequestContext(dto, EndpointAttributes.HttpGet));
            Assert.That(result2.Result, Is.TypeOf<ResponseDto>());

            controller.ExecuteOneWay(dto);
            Assert.True(OneWayService.oneWayWasCalled);

            Assert.That(controller.OperationTypes.Count, Is.EqualTo(1));
            Assert.That(controller.OperationTypes, Contains.Item(typeof(RequestDto5)));

            Assert.That(controller.ServiceTypes.Count, Is.EqualTo(1));
            Assert.That(controller.ServiceTypes, Contains.Item(typeof(OneWayService)));

            Assert.That(controller.RequestTypes.Count, Is.EqualTo(1));
            Assert.That(controller.RequestTypes, Contains.Item(typeof(RequestDto5)));

            Assert.That(controller.RequestServiceTypeMap.Count, Is.EqualTo(1));
            Assert.AreEqual(typeof(OneWayService), controller.RequestServiceTypeMap[typeof(RequestDto5)]);

            Assert.That(controller.ResponseServiceTypeMap, Is.Empty); //We don't follow the response DTO naming convention
        }

        class RequestDto6 { }
        class CustomResponse { }
        class TestService : IService<RequestDto6>
        {
            public object Execute(RequestDto6 request)
            {
                return new ResponseDto();
            }
        }


        [Test]
        public void Response_binders_are_executed()
        {
            var factoryMock = new Mock<ITypeFactory>();
            factoryMock.Setup(x => x.CreateInstance(typeof(TestService))).Returns(new TestService());

            IServiceController controller = new ServiceController();
            controller.RegisterService(factoryMock.Object, typeof(TestService));
            controller.ResponseBinders[typeof(ResponseDto)] = (service, request, response) => new CustomResponse();

            var dto = new RequestDto6();
            var result = controller.ExecuteAsync(dto, s => { });
            Assert.That(result.Result, Is.TypeOf<CustomResponse>());
        }

        [Test]
        public void Async_Response_binders_are_executed()
        {
            var factoryMock = new Mock<ITypeFactory>();
            factoryMock.Setup(x => x.CreateInstance(typeof(TestService))).Returns(new TestService());

            IServiceController controller = new ServiceController();
            controller.RegisterService(factoryMock.Object, typeof(TestService));
            controller.AsyncResponseBinders.Add((service, request, response, callback) =>
            {
                var serviceResult = new SyncServiceResult(new CustomResponse());
                callback(serviceResult);
                return serviceResult;
            });

            var dto = new RequestDto6();
            var result = controller.ExecuteAsync(dto, s => { });
            Assert.That(result.Result, Is.TypeOf<CustomResponse>());
        }
	}
}
