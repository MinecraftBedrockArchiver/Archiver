using System.Web;
using MicrosoftAuth.Models;
using MicrosoftAuth.Models.Token;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MicrosoftAuth
{
    public class MicrosoftAccount
    {
        public string CID { get; set; }
        public string PUID { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Username { get; set; }

        public LegacyToken DaToken { get; set; }

        public MicrosoftDevice? Device { get; set; }

        public Dictionary<string, BaseToken> TokenCache { get; set; }

        public MicrosoftAccount()
        {
            TokenCache = new Dictionary<string, BaseToken>();
        }

        public static MicrosoftAccount FromOAuthResponse(string oauthResponse)
        {
            var account = new MicrosoftAccount();

            var tokens = new List<JToken>();
            var rootObject = JObject.Parse(oauthResponse);

            foreach (var token in rootObject)
            {
                rootObject[token.Key] = HttpUtility.UrlDecode(token.Value!.Value<string>());
            }

            var parsedResponse = rootObject.ToObject<OAuthResponse>();
            account.CID = parsedResponse.CID;
            account.PUID = parsedResponse.PUID;
            account.Firstname = parsedResponse.FirstName;
            account.Lastname = parsedResponse.LastName;
            account.Username = parsedResponse.Username;

            account.DaToken = new LegacyToken(parsedResponse.DAToken, parsedResponse.DASessionKey, parsedResponse.DAStartTime, parsedResponse.DAExpires);

            return account;
        }

        public async Task<BaseToken> RequestToken(string appId, SecureScope scope)
        {
            if (TokenCache.ContainsKey(scope.Address))
            {
                var token = TokenCache[scope.Address];
                if (!token.IsExpired())
                    return token;
            }

            if (Device == null)
            {
                Device = MicrosoftDevice.GenerateDevice();
                await Device.AuthenticateDevice();
            }

            var request = new TokenRequest(DaToken, Device.AuthToken, appId, scope);

            var response = await request.SendRequest();

            foreach (TokenResponse tokenResponse in response.Tokens)
            {
                TokenCache[tokenResponse.Token.TokenScope.Address] = tokenResponse.Token;
            }

            return TokenCache[scope.Address];

        }
    }
}