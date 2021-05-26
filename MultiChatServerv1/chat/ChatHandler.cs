#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrimeAPI.com.brimelive.api;
using Newtonsoft.Json;
using WatsonWebsocket;

namespace MultiChatServer.chat {
    public abstract class ChatHandler {

        public ChatHandler(ChatServer chatServer) {
            this.ChatServer = chatServer;
        }

        private ChatServer ChatServer { get; set; }
        public static int Timeout { get; set; } = 30000;

        protected static readonly string CHAT_FORMAT = "{{\"type\": \"CHAT\", \"sender\": {{\"displayname\": {0}, \"color\": {1}, \"badges\": {2}}}, \"emotes\": {3}, \"message\": {4}, \"mstimeout\": {5} }}";
        protected static readonly string FOLLOW_FORMAT = "{{\"type\": \"FOLLOW\", \"username\": {0} }}";
        protected static readonly string SUB_FORMAT = "{{\"type\": \"SUBSCRIBE\", \"username\": {0}, \"isResub\": {1} }}";

        protected static readonly string DEFAULT_COLOR = "#FFFFFF";

        public bool isConnected { get; set; } = false;

        public void doChatMessage(string sender, string message, string[] emotes, string[] badges, string color) {

            string msg = string.Format(CHAT_FORMAT,
                JsonConvert.ToString(sender),   // msg.Sender.DisplayName
                JsonConvert.ToString(color),         // msg.sender.color
                JSONUtil.ToString(badges),                      // msg.Sender.Badges (array of img link)
                JSONUtil.ToString(emotes),               // msg.Emotes
                JsonConvert.ToString(message),
                JsonConvert.ToString(Timeout)                   // default 30s timeout
                );
            ChatServer.send(msg);
        }

        public void doFollow(string username) {
            string msg = string.Format(FOLLOW_FORMAT,
                username);
            ChatServer.send(msg);
        }

        public void doSubscribe(string username, bool isResub) {
            string msg = string.Format(SUB_FORMAT,
                username,
                isResub);
            ChatServer.send(msg);
        }

        public abstract long getViewerCount();
    }
}
