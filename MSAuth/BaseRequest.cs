using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MicrosoftAuth
{
    internal abstract class BaseRequest<T> where T: class
    {
        protected readonly Uri Address;

        protected BaseRequest(string requestUrl)
        {
            Address = new Uri(requestUrl);
        }

        internal async Task<T> SendRequest()
        {
            var bodyBuilder = new StringBuilder();
            await using var xmlWriter = XmlWriter.Create(bodyBuilder, new XmlWriterSettings { Async = true });

            //await xmlWriter.WriteStartDocumentAsync();

            var bodyElement = WriteRequestBody();

            await bodyElement.SaveAsync(xmlWriter, CancellationToken.None);

            //await xmlWriter.WriteEndDocumentAsync();
            await xmlWriter.FlushAsync();

            var body = bodyBuilder.ToString();

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, Address)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/xml")
            };

            var httpResponse = await client.SendAsync(request);
            var httpResponseContent = await httpResponse.Content.ReadAsStringAsync();

            return ParseResponse(httpResponseContent);
        }

        protected abstract XDocument WriteRequestBody();

        protected abstract T ParseResponse(string responseText);
    }
}