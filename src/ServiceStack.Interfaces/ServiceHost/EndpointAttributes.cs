using System;

namespace ServiceStack.ServiceHost
{
    [Flags]
    public enum EndpointAttributes
    {
        None = 0,

        All = AllNetworkAccessTypes | AllSecurityModes | AllHttpMethods | Soap,
        AllNetworkAccessTypes = External | Localhost,
        AllSecurityModes = Secure | InSecure,
        AllHttpMethods = HttpHead | HttpGet | HttpPost | HttpPut | HttpDelete,
        Soap = Soap11 | Soap12,

        //Whether it came from an Internal or External address
        Localhost = 1 << 0,
        External = 1 << 2,

        //Called over a secure or insecure channel
        Secure = 1 << 3,
        InSecure = 1 << 4,

        //HTTP request type
        HttpHead = 1 << 5,
        HttpGet = 1 << 6,
        HttpPost = 1 << 7,
        HttpPut = 1 << 8,
        HttpDelete = 1 << 9,
        HttpPatch = 1 << 10,
        //Future 11,12

        //Different endpoints
        Soap11 = 1 << 15,
        Soap12 = 1 << 16
    }
}