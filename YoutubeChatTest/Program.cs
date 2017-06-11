using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeChat;

namespace YoutubeChatTest
{
    class Program
    {
        private const string YoutubeClientId = "";
        private const string YoutubeClientSecret = "";
        private const string GoogleApiKey = "";

        [STAThread]
        static void Main(string[] args)
        {
            //Login With Bot User
            YoutubeLogin botLogin = new YoutubeLogin(YoutubeClientId, YoutubeClientSecret);
            botLogin.ShowDialog();

            //Login With Channel User
            YoutubeLogin channelLogin = new YoutubeLogin(YoutubeClientId, YoutubeClientSecret);
            channelLogin.ShowDialog();
            (string channelAccessToken, _) = channelLogin.RequestTokens();


            YoutubeLiveChat<MyUser> youtube = new YoutubeLiveChat<MyUser>(botLogin, GoogleApiKey);


            youtube.LoadLiveChatId(channelAccessToken); //Only working with a live "Youtube Event"(Planned LiveStream)

            youtube.Userlist.Load("userlist.xml");

            youtube.ChannelMessage += (o, e) =>
            {
                Console.WriteLine(e.From.DisplayName + ": " + e.Message + "(Created "+e.From.Created.ToString() +")");
                if (e.Message == "!test") youtube.Send("test received");
            };

            youtube.Start();

            //Stop and Save
            Console.ReadLine();
            youtube.Stop();
            youtube.Userlist.Save("userlist.xml");
        }
    }



    public class MyUser : LiveUser
    {
        private DateTime created;

        public MyUser()
        {
            created = DateTime.Now;
        }


        public override void Update() //called every 10 seconds
        {
            //created = created.AddSeconds(1); //makes no sense but works
        }

        public DateTime Created { get => created; set => created = value; }
    }
}
