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

    public class ChatEmote : JSONConvertable {
        public string Name { get; set; }
        public string Link { get; set; }

        public ChatEmote(string name, string link) {
            this.Name = name;
            this.Link = link;
        }

        public string toJSON() {
            StringBuilder _result = new StringBuilder();
            _result.Append("{")
                .Append(Name.toJSON("name")).Append(", ")
                .Append(Link.toJSON("link"))
                .Append("}");
            return _result.ToString();
        }
    }

    public abstract class ChatHandler {

        public ChatHandler(ChatServer chatServer) {
            this.ChatServer = chatServer;
        }

        private ChatServer ChatServer { get; set; }
        public static int Timeout { get; set; } = 30000;

        protected static readonly string DEFAULT_COLOR = "#FFFFFF";

        public bool isConnected { get; set; } = false;

        public void doChatMessage(string sender, string message, ChatEmote[] emotes, string[] badges, string color, string messageID) {
            // "{{\"type\": \"CHAT\", \"sender\": {{\"displayname\": {0}, \"color\": {1}, \"badges\": {2}}}, \"emotes\": {3}, \"message\": {4}, \"mstimeout\": {5}, \"messageID\": {6} }}"
            StringBuilder _msg = new StringBuilder();
            string emoteStr = emotes.toJSON<ChatEmote>("emotes");
            _msg.Append("{")
                .Append("CHAT".toJSON("type")).Append(", ")
                .Append(sender.toJSON("sender")).Append(", ")
                .Append(color.toJSON("color")).Append(", ")
                .Append(badges.toJSON<string>("badges")).Append(", ")
                .Append(emotes.toJSON<ChatEmote>("emotes")).Append(", ")
                .Append(message.toJSON("message")).Append(", ")
                .Append(Timeout.toJSON("mstimeout")).Append(", ")
                .Append(messageID.toJSON("messageID"))
                .Append("}");
            ChatServer.send(_msg.ToString());
        }

        public void doClearMessage(string messageID) {
            // "{{ \"type\": \"CLEAR\", \"messageID\": {0} }}";
            StringBuilder _msg = new StringBuilder();
            _msg.Append("{")
                .Append("CLEAR".toJSON("type")).Append(", ")
                .Append(messageID.toJSON("messageID"))
                .Append("}");
            ChatServer.send(_msg.ToString());
        }

        public void doFollow(string username) {
            // "{{\"type\": \"FOLLOW\", \"username\": {0}, \"mstimeout\": {1} }}"
            StringBuilder _msg = new StringBuilder();
            _msg.Append("{")
                .Append("FOLLOW".toJSON("type")).Append(", ")
                .Append(username.toJSON("username")).Append(", ")
                .Append(Timeout.toJSON("mstimeout"))
                .Append("}");
            ChatServer.send(_msg.ToString());
        }

        public void doSubscribe(string username, bool isResub, bool isGift, int months) {
            // "{{\"type\": \"SUBSCRIBE\", \"username\": {0}, \"isResub\": {1}, \"isGift\": {2}, \"months\": {3}, \"mstimeout\": {4} }}"
            StringBuilder _msg = new StringBuilder();
            _msg.Append("{")
                .Append("SUBSCRIBE".toJSON("type")).Append(", ")
                .Append(username.toJSON("username")).Append(", ")
                .Append(isResub.toJSON("isResub")).Append(", ")
                .Append(isGift.toJSON("isGift")).Append(", ")
                .Append(months.toJSON("months")).Append(", ")
                .Append(Timeout.toJSON("mstimeout"))
                .Append("}");
            ChatServer.send(_msg.ToString());
        }

        public void doRaid(string channelName, int viewerCount) {
            // "{{ \"type\": \"RAID\", \"channelName\": {0}, \"viewerCount\": {1}, \"mstimeout\": {2} }}"
            StringBuilder _msg = new StringBuilder();
            _msg.Append("{")
                .Append("RAID".toJSON("type")).Append(", ")
                .Append(channelName.toJSON("channelName")).Append(", ")
                .Append(viewerCount.toJSON("viewerCount")).Append(", ")
                .Append(Timeout.toJSON("mstimeout"))
                .Append("}");
            ChatServer.send(_msg.ToString());
        }

        /// <summary>
        /// Used for custom alerts - ie for Twitch would include Bits and Channel Points
        /// </summary>
        /// <param name="alertMsg">Alert message to send</param>
        public void doSpecialAlert(string alertMsg) {
            // "{{\"type\": \"ALERT\", \"alert\": {0}, \"mstimeout\": {1} }}"
            StringBuilder _msg = new StringBuilder();
            _msg.Append("{")
                .Append("ALERT".toJSON("type")).Append(", ")
                .Append(alertMsg.toJSON("alert")).Append(", ")
                .Append(Timeout.toJSON("mstimeout"))
                .Append("}");
            ChatServer.send(_msg.ToString());
        }

        public abstract long getViewerCount();

        public abstract void updateCategory(string category);

        public abstract void updateTitle(string title);
    }
}
