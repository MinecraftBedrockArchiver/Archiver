using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;

namespace MSAuth.Popup
{
    public partial class OAuthPopup : Form
    {
        private static readonly string authPopupUrl = "https://login.live.com/ppsecure/InlineConnect.srf?id=80604&platform=android2.1.0510.1018&client_id=android-app://com.mojang.minecraftearth.H62DKCBHJP6WXXIV7RBFOGOL4NAK4E6Y";

        private static string OAuthResponseToken { get; set; } = "";

        public OAuthPopup()
        {
            Cef.Initialize(new CefSettings());
            InitializeWebview();

            usedBrowser!.LoadUrl(authPopupUrl);
        }

        #region CefSharp Webview Code

        private ChromiumWebBrowser usedBrowser;
        private void InitializeWebview()
        {
            usedBrowser = new ChromiumWebBrowser();
            SuspendLayout();

            usedBrowser.ActivateBrowserOnCreation = false;
            usedBrowser.Dock = DockStyle.Fill;
            usedBrowser.Location = new Point(0, 0);
            usedBrowser.Name = "Microsoft OAuth Popup";
            usedBrowser.Size = new Size(784, 661);
            usedBrowser.TabIndex = 0;
            usedBrowser.Text = "msAuthPopupWebview";
            usedBrowser.LoadingStateChanged += BrowserOnLoadingStateChanged;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 661);
            Controls.Add(usedBrowser);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "MsAuthPopup";
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Microsoft OAuth Popup";

            ResumeLayout(false);
        }

        private void BrowserOnLoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
        {
            if (e.IsLoading) return;

            var cookieVisitor = new CookieVisitor(cookies =>
            {
                var hasFinishedAuth = cookies.Any(cookie => cookie.name == "PPInlineAuth");

                if (hasFinishedAuth)
                {
                    // Mismatched names because on auth finish PPInlineAuth is set,
                    // but Property is populated
                    var propertyCookie = cookies.First(pred => pred.name == "Property");
                    OAuthResponseToken = propertyCookie.value;
                    DialogResult = DialogResult.OK;
                    BeginInvoke(Close);
                }
            });

            Cef.GetGlobalCookieManager().VisitAllCookies(cookieVisitor);
        }

        public static string GetAuthToken()
        { 
            var popup = new OAuthPopup();
            popup.ShowDialog();

            return OAuthResponseToken;
        }

        #endregion
    }
}
