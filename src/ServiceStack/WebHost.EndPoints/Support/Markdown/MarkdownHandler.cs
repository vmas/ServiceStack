using System.Net;
using System.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Formats;

namespace ServiceStack.WebHost.Endpoints.Support.Markdown
{
	public class MarkdownHandler : HttpHandlerBase
	{
		public MarkdownFormat MarkdownFormat { get; set; }
		public MarkdownPage MarkdownPage { get; set; }

		public string PathInfo { get; set; }
		public string FilePath { get; set; }

		public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			var contentPage = MarkdownPage;
			if (contentPage == null)
			{
				var pageFilePath = this.FilePath.WithoutExtension();
				contentPage = MarkdownFormat.GetContentPage(pageFilePath);
			}
			if (contentPage == null)
			{
				httpRes.StatusCode = (int) HttpStatusCode.NotFound;
				return;
			}

			MarkdownFormat.ReloadModifiedPageAndTemplates(contentPage);

			if (httpReq.DidReturn304NotModified(contentPage.GetLastModified(), httpRes))
				return;

			MarkdownFormat.ProcessMarkdownPage(httpReq, contentPage, null, httpRes);
		}
    }
}