namespace MicrosoftAuth.Models
{
    internal class OAuthResponse
    {
        public string AuthenticationBuffer { get; set; }
        public string Password { get; set; }
        public string DAToken { get; set; }
        public string DASessionKey { get; set; }
        public DateTime DAStartTime { get; set; }
        public DateTime DAExpires { get; set; }
        public string STSInlineFlowToken { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CID { get; set; }
        public string PUID { get; set; }
        public string Username { get; set; }
        public string SigninName { get; set; }
        public string BackURL { get; set; }

    }
}