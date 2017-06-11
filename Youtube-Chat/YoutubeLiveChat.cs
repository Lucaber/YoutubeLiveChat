using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YoutubeChat
{
    public class YoutubeLiveChat<T> where T : LiveUser,new()
    {
        private String apiKey;
        private String liveChatId;
        private String accessToken;
        private String refreshToken;
        private YoutubeLogin login;

        private LiveUserList<T> userlist = new LiveUserList<T>();

        private String nextPageToken = "";
        private int pollingIntervalMillis;

        public event ChannelMessageHandler ChannelMessage;
        public delegate void ChannelMessageHandler(object sender, MessageEventArgs<T> e);

        private Thread mainThread, tokenRefreshThread;

        public string ApiKey { get => apiKey; set => apiKey = value; }
        public string LiveChatId { get => liveChatId; set => liveChatId = value; }
        public LiveUserList<T> Userlist { get => userlist; set => userlist = value; }

        public YoutubeLiveChat(YoutubeLogin login, string apiKey, string accessToken="", string refreshToken="", string liveChatId = "")
        {
            this.login = login;
            this.ApiKey = apiKey;

            if (accessToken == "" && refreshToken == "")
                (this.accessToken,this.refreshToken) = login.RequestTokens();
            else
            {
                if (accessToken != "") this.accessToken = accessToken;
                else this.accessToken = login.RefreshToken(refreshToken);

                this.refreshToken = refreshToken;
            }
            
            this.LiveChatId = liveChatId;
        }


        public void Start()
        {
            mainThread = new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    try
                    {
                        LoadMessages();
                        Thread.Sleep(pollingIntervalMillis);
                    }
                    catch (ThreadAbortException) { }
                }
            }));
            mainThread.Start();
            
            tokenRefreshThread = new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    try
                    {
                        Thread.Sleep(50 * 60 * 1000);
                        this.accessToken = login.RefreshToken(refreshToken);
                    }
                    catch (ThreadAbortException) { }
                }
            }));
            tokenRefreshThread.Start();
            userlist.Start();
        }

        public void Stop()
        {
            mainThread.Abort();
            tokenRefreshThread.Abort();
            userlist.Stop();
        }


        public void Send(string message)
        {
            WebClient wc = new WebClient();
            wc.QueryString.Add("part", "snippet");
            wc.QueryString.Add("key", apiKey);
            //wc.QueryString.Add("access_token", accessToken);
            wc.QueryString.Add("mine", "true");
            wc.Headers.Add("Authorization", "Bearer " + accessToken);
            wc.Headers[HttpRequestHeader.ContentType] = "application/json";
            wc.UploadString("https://www.googleapis.com/youtube/v3/liveChat/messages", $"{{\"snippet\": {{\"liveChatId\": \"{liveChatId}\",\"type\": \"textMessageEvent\",\"textMessageDetails\": {{\"messageText\": \"{message}\"}}}}}}");
        }

        public void LoadMessages()
        {
            bool first = false;
            WebClient wc = new WebClient();
            wc.QueryString.Add("liveChatId", LiveChatId);
            wc.QueryString.Add("part", "snippet,authorDetails");
            wc.QueryString.Add("key", apiKey);
            if (nextPageToken != "") wc.QueryString.Add("pageToken", nextPageToken);
            else first = true;

            String stringresponse = wc.DownloadString("https://www.googleapis.com/youtube/v3/liveChat/messages");

            var response = JsonConvert.DeserializeObject<YoutubeListResponse>(stringresponse);
            nextPageToken = response.nextPageToken;
            pollingIntervalMillis = response.pollingIntervalMillis;
            if (!first) foreach (YoutubeListItem i in response.items)
            {
                    T user = Userlist.UserByChannelId(i.snippet.authorChannelId);
                    if (user == null)
                    {
                        user = (T)Activator.CreateInstance(typeof(T));
                       // user = new T();
                        user.ChannelId = i.snippet.authorChannelId;
                        user.DisplayName = i.authorDetails.displayName;
                        //user = new T(i.snippet.authorChannelId, i.authorDetails.displayName);
                        Userlist.Add(user);
                    }


                 ChannelMessage(this, new MessageEventArgs<T>(user, i.snippet.textMessageDetails.messageText));
            }
        }
        public string LoadLiveChatId(string accessToken="")
        {
            if (accessToken == "") accessToken = this.accessToken;
            WebClient wc = new WebClient();
            wc.QueryString.Add("part", "snippet");
            wc.QueryString.Add("key", apiKey);
            wc.QueryString.Add("mine", "true");
            wc.Headers.Add("Authorization", "Bearer " + accessToken);
            String stringresponse = wc.DownloadString("https://www.googleapis.com/youtube/v3/liveBroadcasts");

            dynamic response = JsonConvert.DeserializeObject(stringresponse);
            string lci = response.items[0].snippet.liveChatId;
            if (lci == "" || lci == null) throw new Exception("No Livestream found");
            LiveChatId = lci;
            return lci;
        }
    }


    public class MessageEventArgs<T> : EventArgs where T : LiveUser
    {
        private T from;
        private String message;

        public MessageEventArgs(T from, string message)
        {
            this.From = from;
            this.Message = message;
        }

        public T From { get => from; set => from = value; }
        public string Message { get => message; set => message = value; }

    }


    #region YoutubeListResponse

    public class YoutubeListResponse
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public string nextPageToken { get; set; }
        public int pollingIntervalMillis { get; set; }
        public YoutubeListPageinfo pageInfo { get; set; }
        public YoutubeListItem[] items { get; set; }
    }

    public class YoutubeListPageinfo
    {
        public int totalResults { get; set; }
        public int resultsPerPage { get; set; }
    }

    public class YoutubeListItem
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public string id { get; set; }
        public YoutubeListSnippet snippet { get; set; }
        public YoutubeListAuthordetails authorDetails { get; set; }
    }

    public class YoutubeListSnippet
    {
        public string type { get; set; }
        public string liveChatId { get; set; }
        public string authorChannelId { get; set; }
        public DateTime publishedAt { get; set; }
        public bool hasDisplayContent { get; set; }
        public string displayMessage { get; set; }
        public YoutubeListTextmessagedetails textMessageDetails { get; set; }
    }

    public class YoutubeListTextmessagedetails
    {
        public string messageText { get; set; }
    }

    public class YoutubeListAuthordetails
    {
        public string channelId { get; set; }
        public string channelUrl { get; set; }
        public string displayName { get; set; }
        public string profileImageUrl { get; set; }
        public bool isVerified { get; set; }
        public bool isChatOwner { get; set; }
        public bool isChatSponsor { get; set; }
        public bool isChatModerator { get; set; }
    }

    #endregion
}
