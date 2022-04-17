using MicrosoftAuth.Models;
using MicrosoftAuth.Models.Token;

namespace MicrosoftAuth
{
    public class MicrosoftDevice
    {
        public LegacyToken? AuthToken { get; set; }

        public string Name { get; set; }
        public string Password { get; set; }
        public string? DevicePuid { get; set; }

        public MicrosoftDevice()
        {
            Name = "";
            Password = "";
        }

        public async Task<LegacyToken> AuthenticateDevice()
        {
            if (AuthToken != null && !AuthToken.IsExpired())
                return AuthToken;

            if (DevicePuid == null)
                await RegisterDevice();

            var request = new AuthenticateDeviceRequest(Name, Password);

            var response = await request.SendRequest();

            AuthToken = response.DeviceToken;

            return AuthToken;
        }

        internal static MicrosoftDevice GenerateDevice()
        {
            var device = new MicrosoftDevice();

            string deviceName = "mca_msa_client_";
            string devicePassword = "";

            var rdm = new Random();

            var allowedAlphabet =
                "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ!@#$%^&*()-_=+[]{}/?;:'\\\\\\\",.<>`~";

            for (int i = 0; i < 6; i++)
                deviceName += allowedAlphabet[rdm.Next(36)];

            for (int i = 0; i < 16; i++)
                devicePassword += allowedAlphabet[rdm.Next(allowedAlphabet.Length)];

            device.Name = deviceName;
            device.Password = devicePassword;

            return device;
        }

        private async Task RegisterDevice()
        {
            var request = new AddDeviceRequest(Name, Password);

            var response = await request.SendRequest();

            DevicePuid = response.Puid;
        }
    }
}