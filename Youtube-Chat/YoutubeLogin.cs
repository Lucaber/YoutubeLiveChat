using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace YoutubeChat
{
    public partial class YoutubeLogin : Form
    {
        private String code;
        private string clientId;
        private string clientSecret;
        public string Code { get => code; set => code = value; }

        public YoutubeLogin(string clientid, string clientSecret)
        {
            this.clientId = clientid;
            this.clientSecret = clientSecret;
            InitializeComponent();
            web.ScriptErrorsSuppressed = true;
        }

        private void YoutubeLogin_Load(object sender, EventArgs e)
        {

            web.Navigate($"https://accounts.google.com/o/oauth2/auth?client_id={clientId}&redirect_uri=urn:ietf:wg:oauth:2.0:oob&response_type=code&scope=https://www.googleapis.com/auth/youtube");
        }

        private void web_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            string t = web.Document.Title;
            if (t.Contains("code="))
            {
                Code = t.Substring(t.IndexOf("code=") + 5);
                Close();
            }
        }

        public (string access_token, string refresh_token) RequestTokens(string code = "")
        {
            if (code == "") code = this.code;
            WebClient wc = new WebClient();
            wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            String stringresponse = wc.UploadString("https://accounts.google.com/o/oauth2/token", $"code={code}&" +
            $"client_id={clientId}&" +
            $"client_secret={clientSecret}&" +
            $"redirect_uri=urn:ietf:wg:oauth:2.0:oob&" +
            $"grant_type=authorization_code");

            RequestTokensResopnse resopnse = JsonConvert.DeserializeObject<RequestTokensResopnse>(stringresponse);
            return (resopnse.access_token, resopnse.refresh_token);
        }

        public string RefreshToken(string refreshToken = "")
        {
            WebClient wc = new WebClient();
            wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            String stringresponse = wc.UploadString("https://accounts.google.com/o/oauth2/token", $"client_id={clientId}&" +
            $"client_secret={clientSecret}&" +
            $"refresh_token={refreshToken}&" +
            $"grant_type=refresh_token");

            RefreshTokenResponse resopnse = JsonConvert.DeserializeObject<RefreshTokenResponse>(stringresponse);
            return resopnse.access_token;
        }
    }


    public class RefreshTokenResponse
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
    }

    public class RequestTokensResopnse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
    }

}
