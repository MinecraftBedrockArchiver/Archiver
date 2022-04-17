using System.Xml.Linq;
using MicrosoftAuth.Models.Token;

namespace MicrosoftAuth.Models
{
    public class TokenResponse
    {
        public bool IsLegacy { get; set; }
        public BaseToken? Token { get; set; }
        public TokenErrorInfo? TokenError { get; set; }

        public static TokenResponse FromElement(XElement element)
        {
            try
            {
                var token = BaseToken.ParseTokenFromElement(element);
                return new TokenResponse(token);
            }
            catch (Exception)
            {
                return new TokenResponse(TokenErrorInfo.FromElement(element));
            }
        }

        private TokenResponse(BaseToken token)
        {
            IsLegacy = token.GetType() == typeof(LegacyToken);
            Token = token;
            TokenError = null;
        }

        private TokenResponse(TokenErrorInfo error)
        {
            IsLegacy = false;
            Token = null;
            TokenError = error;
        }
    }
}