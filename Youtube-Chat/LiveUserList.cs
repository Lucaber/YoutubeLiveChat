using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace YoutubeChat
{
    public class LiveUserList<T> where T : LiveUser
    {
        private List<T> userlist = new List<T>();
        Thread mainThread,updateThread;
        bool stoprequest;
        public T UserByChannelId(string channelId)
        {
            var erg = userlist.Where((user) => { return user.ChannelId == channelId; });
            if (erg.Count() > 0) return erg.First();
            return null;
        }


        public void Add(T user)
        {
            userlist.Add(user);
        }

        public void Update()
        {
            if (updateThread?.ThreadState == ThreadState.Running) return;
            updateThread = new Thread(new ThreadStart(() => {
                try
                {
                    userlist.ForEach((user) => { ((T)user).Update(); });
                }
                catch { }
            }));
            updateThread.Start();
        }


        public void Start()
        {
            stoprequest = false;
            if (mainThread?.ThreadState != ThreadState.Running)
            {
                mainThread = new Thread(new ThreadStart(() => {
                    while (!stoprequest)
                    {
                        try
                        {
                            this.Update();
                            System.Threading.Thread.Sleep(1000 * 10);
                        }
                        catch { }
                    }
                    stoprequest = false;
                }));
                mainThread.Start();
            }
        }

        public void Stop()
        {
            stoprequest = true;
            System.Threading.Thread.Sleep(200);
            mainThread.Abort();
        }


        public void Save(string file = "user.xml")
        {

            XmlSerializer xmlser = new XmlSerializer(typeof(List<T>));
            TextWriter writer = new StreamWriter(file);
            xmlser.Serialize(writer, userlist);
            writer.Close();
        }

        public void Load(string file = "user.xml")
        {
            if (System.IO.File.Exists(file))
            {
                XmlSerializer xmlser = new XmlSerializer(typeof(List<T>));
                StreamReader reader = new StreamReader(file);
                userlist = (List<T>)xmlser.Deserialize(reader);
                reader.Close();
            }
        }


    }
}
