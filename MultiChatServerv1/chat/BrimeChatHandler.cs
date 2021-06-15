#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrimeAPI.com.brimelive.api;
using BrimeAPI.com.brimelive.api.channels;
using BrimeAPI.com.brimelive.api.emotes;
using BrimeAPI.com.brimelive.api.errors;
using BrimeAPI.com.brimelive.api.realtime;
using Newtonsoft.Json;

namespace MultiChatServer.chat {
    public class BrimeChatHandler : ChatHandler, BrimeRealtimeListener {
        protected static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public BrimeRealtimeAPI? BrimeRealtime { get; private set; }
        private string BrimeName { get; set; }

        public BrimeChatHandler(ChatServer server, ChatServerSettings settings) : base(server) {
            this.BrimeName = settings.BrimeName;
            if (string.IsNullOrWhiteSpace(BrimeAPI.com.brimelive.api.BrimeAPI.ClientID) && !string.IsNullOrWhiteSpace(settings.BrimeChannelID)) {
                BrimeRealtime = new BrimeRealtimeAPI(settings.BrimeName);
            } else {
                try {
                    if (string.IsNullOrWhiteSpace(settings.BrimeChannelID)) {
                        ChannelRequest req = new ChannelRequest(settings.BrimeName);
                        settings.BrimeChannelID = req.getResponse().ID;
                    }
                    BrimeRealtime = new BrimeRealtimeAPI(settings.BrimeChannelID);
                } catch (BrimeAPIException e) {
                    Logger.Error(e.ToString());
                    string cid = BrimeAPI.com.brimelive.api.BrimeAPI.ClientID;
                    BrimeAPI.com.brimelive.api.BrimeAPI.ClientID = "";
                    BrimeRealtime = new BrimeRealtimeAPI(settings.BrimeName);
                    BrimeAPI.com.brimelive.api.BrimeAPI.ClientID = cid;
                } catch (Exception e) {
                    Logger.Error(e.ToString());
                    throw e;
                }
            }
            BrimeRealtime.registerListener(this);
            Logger.Info("Connecting BrimeAPI <" + BrimeName + ">");
            BrimeRealtime.connect();
            isConnected = true;
        }

        public override long getViewerCount() {
            return (BrimeRealtime == null ? 0 : BrimeRealtime.ViewCountTracker.ViewCount);
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

        public void onChat(BrimeChatMessage chatMessage) {
            string[] badges = new string[chatMessage.Sender.Badges.Count + 1];
            badges[0] = "http://localhost:8080/BrimeLogo.png";
            chatMessage.Sender.Badges.ToArray().CopyTo(badges, 1);

            List<ChatEmote> emotes = new List<ChatEmote>(chatMessage.Emotes.Count);
            foreach (string e in chatMessage.Emotes.Keys) {
                emotes.Add(new ChatEmote(e, BrimeEmote.getImageURL(chatMessage.Emotes[e].EmoteID, BrimeEmoteSize.X1)));
            }

            doChatMessage(chatMessage.Sender.DisplayName, chatMessage.Message, emotes.ToArray(), badges, chatMessage.Sender.Color, "BRIME:"+chatMessage.ID);
        }

        public void onDeleteChat(string messageID) {
            doClearMessage("BRIME:"+messageID);
        }

        public void onSubscribe(string username, string userId, bool isResub) {
            doSubscribe(username, isResub, false, -1);
        }

        public void onRaid(string channelName, string channelID, int viewerCount) {
            doRaid(channelName, viewerCount);
        }
    }
}
