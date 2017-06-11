using System;

namespace YoutubeChat
{
    public class LiveUser
    {

        private String channelId;
        private String displayName;

        public string ChannelId { get => channelId; set => channelId = value; }
        public string DisplayName { get => displayName; set => displayName = value; }

        virtual public void Update() //called every 10 seconds
        {
            
        }
    }
}
