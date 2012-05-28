using System.Runtime.Serialization;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceHost;

namespace ServiceStack.Messaging.Tests.Services
{
	[DataContract]
	public class Greet
	{
		[DataMember]
		public string Name { get; set; }
	}

	[DataContract]
	public class GreetResponse
	{
		[DataMember]
		public string Result { get; set; }
	}

	public class GreetService
		: IOneWayService<Greet>
	{
		public int TimesCalled { get; set; }
		public string Result { get; set; }

        public void ExecuteOneWay(Greet request)
        {
            this.TimesCalled++;

            Result = "Hello, " + request.Name;
        }
    }

}