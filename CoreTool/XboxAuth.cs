using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoreTool
{
	internal class XboxAuth
	{
		private static IPublicClientApplication App;

		private static HttpClient HttpClient = new HttpClient();

		public static readonly Log GenericLogger = new Log("XboxAuth");

		private static void SetupApp()
		{
			App = PublicClientApplicationBuilder
				.Create("b3900558-4f9d-43ef-9db5-cfc7cb01874e")
				.WithAuthority("https://login.microsoftonline.com/consumers")
				.WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
				.Build();

			new TokenCacheHelper("msal_cache.json").EnableSerialization(App.UserTokenCache);
		}

		private static async Task<AuthenticationResult> GetAuthenticationResult()
		{
			AuthenticationResult msalResult = null;

			var accounts = await App.GetAccountsAsync();
			try
			{
				msalResult = await App.AcquireTokenSilent(["XboxLive.signin"], accounts.FirstOrDefault())
									  .ExecuteAsync();
				GenericLogger.Write("Got auth from cache");
			}
			catch (MsalUiRequiredException)
			{
				// Device code flow
				msalResult = await App.AcquireTokenWithDeviceCode(["XboxLive.signin"], callback =>
				{
					GenericLogger.Write(callback.Message);
					return Task.CompletedTask;
				}).ExecuteAsync();
			}

			return msalResult;
		}

		private static async Task<string> GetXblToken(string accessToken)
		{
			var response = await HttpClient.PostAsync("https://user.auth.xboxlive.com/user/authenticate", new StringContent(
				$"{{\"Properties\":{{\"AuthMethod\":\"RPS\",\"SiteName\":\"user.auth.xboxlive.com\",\"RpsTicket\":\"d={accessToken}\"}},\"RelyingParty\":\"http://auth.xboxlive.com\",\"TokenType\":\"JWT\"}}",
				Encoding.UTF8, "application/json"));

			var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
			return json.GetProperty("Token").GetString();
		}

		private static async Task<(string uhs, string xstsToken)> GetXstsToken(string xblToken)
		{
			var response = await HttpClient.PostAsync("https://xsts.auth.xboxlive.com/xsts/authorize", new StringContent(
				$"{{\"Properties\":{{\"SandboxId\":\"RETAIL\",\"UserTokens\":[\"{xblToken}\"]}},\"RelyingParty\":\"http://update.xboxlive.com\",\"TokenType\":\"JWT\"}}",
				Encoding.UTF8, "application/json"));

			var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
			var uhs = json.GetProperty("DisplayClaims").GetProperty("xui")[0].GetProperty("uhs").GetString();

			return (uhs, json.GetProperty("Token").GetString());
		}

		public static async Task<string> GetXboxToken()
		{
			if (App == null)
				SetupApp();

			var msalResult = await GetAuthenticationResult();

			var xblToken = await GetXblToken(msalResult.AccessToken);

			var (uhs, xstsToken) = await GetXstsToken(xblToken);

			return $"XBL3.0 x={uhs};{xstsToken}";
		}
	}

	public class TokenCacheHelper
	{
		private readonly string _filePath;

		public TokenCacheHelper(string filePath) => _filePath = filePath;

		public void EnableSerialization(ITokenCache tokenCache)
		{
			tokenCache.SetBeforeAccess(args =>
			{
				if (File.Exists(_filePath))
				{
					args.TokenCache.DeserializeMsalV3(File.ReadAllBytes(_filePath));
				}
			});

			tokenCache.SetAfterAccess(args =>
			{
				if (args.HasStateChanged)
				{
					File.WriteAllBytes(_filePath, args.TokenCache.SerializeMsalV3());
				}
			});
		}
	}
}
