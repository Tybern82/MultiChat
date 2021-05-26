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


        public void onOpen() { }

        public void onClose() { }

        public void onFollow(string username, string id) {
            doFollow(username);
        }

        public void onJoin(string username) { }

        public void onLeave(string username) { }

        public void onChat(BrimeChatMessage chatMessage) {
            string[] badges = new string[chatMessage.Sender.Badges.Count + 1];
            badges[0] = "http://localhost:8080/BrimeLogo.png";
            chatMessage.Sender.Badges.ToArray().CopyTo(badges, 1);

            doChatMessage(chatMessage.Sender.DisplayName, chatMessage.Message, new string[0], badges, chatMessage.Sender.Color);
        }

        public void onSubscribe(string username, string userId, bool isResub) {
            doSubscribe(username, isResub);
        }
    }
}
