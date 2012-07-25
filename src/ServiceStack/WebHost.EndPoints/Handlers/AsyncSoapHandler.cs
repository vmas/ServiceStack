using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.Endpoints.Utils;

namespace ServiceStack.WebHost.Endpoints.Handlers
{
    public class AsyncSoapHandler : AsyncHandlerBase
    {
        private MessageVersion version;

        private Message requestMsg;
        private string requestXml;
        private Type requestType;

        public AsyncSoapHandler(MessageVersion version)
        {
            this.version = version;
        }

        private Message GetMessage(IHttpRequest req)
        {
            using (var sr = new StreamReader(req.InputStream))
            {
                var requestXml = sr.ReadToEnd();

                var doc = new XmlDocument();
                doc.LoadXml(requestXml);

                var msg = Message.CreateMessage(new XmlNodeReader(doc), int.MaxValue, version);
                return msg;
            }
        }

        private static string GetOperationName(string contentType)
        {
            var urlActionPos = contentType.IndexOf("action=\"");
            if (urlActionPos != -1)
            {
                var startIndex = urlActionPos + "action=\"".Length;
                var urlAction = contentType.Substring(
                    startIndex,
                    contentType.IndexOf('"', startIndex) - startIndex);

                var parts = urlAction.Split('/');
                var operationName = parts.Last();
                return operationName;
            }

            return null;
        }

        private static string GetAction(IHttpRequest req, Message requestMsg, string xml)
        {
            var action = GetOperationName(req.ContentType);
            if (action != null) return action;

            if (requestMsg.Headers.Action != null)
            {
                return requestMsg.Headers.Action;
            }

            return xml.StartsWith("<")
                ? xml.Substring(1, xml.IndexOf(" ") - 1).SplitOnFirst(':').Last()
                : null;
        }

        private Type GetRequestType(IHttpRequest req, Message requestMsg, string xml)
        {
            var action = GetAction(req, requestMsg, xml);

            var operationType = EndpointHost.ServiceOperations.GetOperationType(action);
            if(operationType == null)
                throw new NotImplementedException(
                        string.Format("The operation '{0}' does not exist for this service", action));

            return operationType;
        }

        public override IServiceResult BeginProcessRequest(IHttpRequest req, IHttpResponse res, Action<IServiceResult> callback)
        {
            try
            {
                requestMsg = GetMessage(req);
                using (var reader = requestMsg.GetReaderAtBodyContents())
                    requestXml = reader.ReadOuterXml();

                requestType = GetRequestType(req, requestMsg, requestXml);
                var requestDto = DataContractDeserializer.Instance.Parse(requestXml, requestType);

                if (EndpointHost.ApplyRequestFilters(req, res, requestDto)) return this.CancelRequestProcessing(callback);

                var endpointAttributes = GetEndpointAttributes(req);
                if (version == MessageVersion.Soap11)
                    endpointAttributes = endpointAttributes | EndpointAttributes.Soap11;
                else
                    endpointAttributes = endpointAttributes | EndpointAttributes.Soap12;

                var requestContext = new HttpRequestContext(req, res, requestDto, endpointAttributes);

                return EndpointHost.ServiceManager.ServiceController.ExecuteAsync(requestDto, callback, requestContext);
            }
            catch (Exception ex)
            {
                this.HandleException(req, res, ex);
                return this.CancelRequestProcessing(callback);
            }
        }

        private string GetSoapContentType(IHttpRequest req, Message requestMsg, string xml)
        {
            var requestOperationName = GetAction(req, requestMsg, xml);
            return requestOperationName != null
                    ? req.ContentType.Replace(requestOperationName, requestOperationName + "Response")
                    : (version == MessageVersion.Soap11 ? ContentType.Soap11 : ContentType.Soap12);
        }

        public override void EndProcessRequest(IHttpRequest req, IHttpResponse res, IServiceResult result)
        {
            try
            {
                if (res.IsClosed)
                    return;

                var responseDto = result.Result;
                if (EndpointHost.ApplyResponseFilters(req, res, responseDto)) return;

                var httpResult = responseDto as IHttpResult;
                if (httpResult != null)
                    responseDto = httpResult.Response;

                var responseMsg = requestMsg.Headers.Action == null
                        ? Message.CreateMessage(version, null, responseDto)
                        : Message.CreateMessage(version, requestType.Name + "Response", responseDto);

                res.ContentType = GetSoapContentType(req, requestMsg, requestXml);
                using (var writer = XmlWriter.Create(res.OutputStream))
                    responseMsg.WriteMessage(writer);
            }
            catch (Exception ex)
            {
                this.HandleException(req, res, ex);
            }
            finally
            {
                if (!res.IsClosed)
                    res.Close();
            }
        }
    }
}
