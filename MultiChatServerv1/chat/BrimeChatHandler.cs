#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrimeAPI.com.brimelive.api;
using BrimeAPI.com.brimelive.api.realtime;
using Newtonsoft.Json;

namespace MultiChatServer.chat {
    public class BrimeChatHandler : ChatHandler, BrimeRealtimeListener {
        protected static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public BrimeRealtimeAPI? BrimeAPI { get; private set; }
        private string BrimeName { get; set; }

        public BrimeChatHandler(string brimeName, ChatServer server) : base(server) {
            this.BrimeName = brimeName;

            BrimeAPI = new BrimeRealtimeAPI(brimeName);
            BrimeAPI.registerListener(this);
            Logger.Info("Connecting BrimeAPI <" + BrimeName + ">");
            BrimeAPI.connect();
            isConnected = true;
        }

        public override long getViewerCount() {
            return (BrimeAPI == null ? 0 : BrimeAPI.ViewCountTracker.ViewCount);
        }

        public override void updateCategory(string category) {
            // TODO: Need Client-ID to set
        }

        public override void updateTitle(string title) {
            // TODO: Need Client-ID to set
        }


        public void onOpen() { }

        public void onClose() { }

        public void onFollow(string username, string id) {
            doFollow(username);
        }

        public void onJoin(string username) { }

        public void onLeave(string username) { }

        private static readonly string EMOTE_FORMAT = "{{ \"name\": \"{0}\", \"link\": \"{1}\" }}";

        public void onChat(BrimeChatMessage chatMessage) {
            string[] badges = new string[chatMessage.Sender.Badges.Count + 1];
            badges[0] = "http://localhost:8080/BrimeLogo.png";
            chatMessage.Sender.Badges.ToArray().CopyTo(badges, 1);

            List<string> emotes = new List<string>(chatMessage.Emotes.Count);
            foreach (string e in chatMessage.Emotes.Keys) {
                emotes.Add(string.Format(EMOTE_FORMAT, e, chatMessage.Emotes[e].get1xImageUrl()));
            }

            doChatMessage(chatMessage.Sender.DisplayName, chatMessage.Message, emotes.ToArray(), badges, chatMessage.Sender.Color);
        }

        public void onSubscribe(string username, string userId, bool isResub) {
            doSubscribe(username, isResub, false, -1);
        }
    }
}
