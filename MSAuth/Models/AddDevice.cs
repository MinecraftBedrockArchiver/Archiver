using System.Xml;
using System.Xml.Linq;

namespace MicrosoftAuth.Models
{
    internal class AddDeviceRequest : BaseRequest<AddDeviceResponse>
    {
        private string Membername { get; set; }
        private string Password { get; set; }

        internal AddDeviceRequest(string deviceName, string devicePassword) : base(AuthenticationConfig.ADD_DEVICE_URL)
        {
            Membername = deviceName;
            Password = devicePassword;
        }

        protected override XDocument WriteRequestBody()
        {
            var doc = new XDocument();
            doc.Add(new XElement("DeviceAddRequest",
                new XElement("ClientInfo",
                    new XAttribute("name", "IDCRL"),
                    new XAttribute("version", "1.0"),
                    new XElement("BinaryVersion", "19")
                ),
                new XElement("Authentication",
                    new XElement("Membername", Membername),
                    new XElement("Password", Password)
                )
            ));

            return doc;
        }

        protected override AddDeviceResponse ParseResponse(string responseText)
        {
            var document = XDocument.Parse(responseText);
            var responseNode = document.Element("DeviceAddResponse");
            var responseStatus = responseNode?.Attribute("Success")?.Value == "true";

            if (responseStatus)
            {
                var puid = responseNode!.Element("puid")?.Value!;
                return new AddDeviceResponse(puid);
            }
            else
            {
                throw new InvalidDataException(
                    $"Failed to parse response while adding new device. Response: {responseText}");
            }
        }
    }

    internal class AddDeviceResponse
    {
        internal string Puid { get; }

        internal AddDeviceResponse(string devicePuid)
        {
            Puid = devicePuid;
        }
    }
}