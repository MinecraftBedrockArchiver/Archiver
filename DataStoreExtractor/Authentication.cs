using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MicrosoftAuth;
using MicrosoftAuth.Models.Token;
using MSAuth.Popup;

namespace DataStoreExtractor
{
    // TODO: Move this to a shared lib? Same as the one in CoreTool
    class Authentication
    {
        public static async Task<string> GetMicrosoftToken(string cacheFile, string scope = "service::dcat.update.microsoft.com::MBI_SSL")
        {
            MicrosoftAccount account = null;

            if (File.Exists(cacheFile))
            {
                await using FileStream readStream = File.OpenRead(cacheFile);
                var cachedAccount = await JsonSerializer.DeserializeAsync<MicrosoftAccount>(readStream);

                if (!cachedAccount.DaToken.IsExpired())
                    account = cachedAccount;
            }

            if (account == null)
            {
                var token = OAuthPopup.GetAuthToken();
                account = MicrosoftAccount.FromOAuthResponse(token);
            }

            var tokenRequest = await account.RequestToken("{28520974-CE92-4F36-A219-3F255AF7E61E}",
                new SecureScope($"scope={scope}", "TOKEN_BROKER"));

            var receivedToken = tokenRequest as CompactToken;

            Console.WriteLine($"[Microsoft Auth] Received token for scope {scope}.");

            try
            {
                await using FileStream writeStream = File.Create(cacheFile);
                await JsonSerializer.SerializeAsync(writeStream, account);
            }
            catch (JsonException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return Convert.ToBase64String(Encoding.Unicode.GetBytes(receivedToken.Token));
        }
    }
}
