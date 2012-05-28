using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.ServiceHost
{
    public interface IOneWayService<T>
    {
        void ExecuteOneWay(T request);
    }
}
