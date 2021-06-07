#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Trovo.Base;
using Trovo.Base.Clients;
using Trovo.Base.Models.Channels;
using Trovo.Base.Models.Chat;
using Trovo.Base.Models.Users;

namespace MultiChatServer.chat {
    public class TrovoChatHandler : ChatHandler {
        protected static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly string clientID = "8FMjuk785AX4FMyrwPTU3B8vYvgHWN33";
        public static readonly List<OAuthClientScopeEnum> scopes = new List<OAuthClientScopeEnum>()
        {
            OAuthClientScopeEnum.chat_connect,
            OAuthClientScopeEnum.chat_send_self,
            OAuthClientScopeEnum.send_to_my_channel,
            OAuthClientScopeEnum.manage_messages,

            OAuthClientScopeEnum.channel_details_self,
            OAuthClientScopeEnum.channel_update_self,
            OAuthClientScopeEnum.channel_subscriptions,

            OAuthClientScopeEnum.user_details_self,
        };

        private TrovoConnection? connection;

        private ChatClient? chat;

        // public string TrovoName { get; set; }

        public TrovoChatHandler(ChatServer server) : base(server) {
            // this.TrovoName = trovoName; 
            Task.Run(async () => {
                try {
                    Logger.Info("Connecting to Trovo...");
                    
                    connection = await TrovoConnection.ConnectViaLocalhostOAuthBrowser(clientID, scopes);
                    if (connection != null) {
                        Logger.Info("Trovo connection successful!");

                        PrivateUserModel user = await connection.Users.GetCurrentUser();
                        if (user != null) {
                            Logger.Info("Current User: " + user.userName);
                            server.TrovoName.Invoke(user.userName);

                            PrivateChannelModel channel = await connection.Channels.GetCurrentChannel();
                            if (channel != null) {
                                Logger.Info("Channel Title: " + channel.live_title);

                                chat = new ChatClient(connection);
                                chat.OnChatMessageReceived += Chat_OnChatMessageReceived;

                                Logger.Info("Connecting to chat...");
                                if (await chat.Connect(await connection.Chat.GetToken())) {
                                    isConnected = true;
                                    Logger.Info("Successfully connected to chat!");
                                }
                                Thread t = new Thread(new ThreadStart(() => {
                                    HashSet<string> existingSubs = new HashSet<string>();
                                    bool complete = false;
                                    int offset = 0;
                                    while (!complete) {
                                        Logger.Trace("Recording existing Trovo subscribers");
                                        IEnumerable<ChannelSubscriberModel> results = connection.Channels.GetSubscribers(channel.channel_id, 100, offset).Result;
                                        int i = 0;
                                        foreach (var sub in results) {
                                            existingSubs.Add(sub.user.user_id);
                                            i++;
                                        }
                                        if (i < 100) complete = true;
                                        offset++;
                                    }
                                    Logger.Trace("Found " + existingSubs.Count + " existing Trovo subscribers");
                                    while (server.RunServer) {
                                        Thread.Sleep(30000);
                                        offset = 0;
                                        complete = false;
                                        while (!complete) {
                                            Logger.Trace("Checking for new Trovo subscribers");
                                            IEnumerable<ChannelSubscriberModel> results = connection.Channels.GetSubscribers(channel.channel_id, 100, offset).Result;
                                            int i = 0;
                                            foreach (var sub in results) {
                                                if (!existingSubs.Contains(sub.user.user_id)) {
                                                    Logger.Trace("Found new Trovo subscriber: " + sub.user.nickname);
                                                    doSubscribe(sub.user.nickname, false, false, -1);
                                                    existingSubs.Add(sub.user.user_id);
                                                }
                                                i++;
                                            }
                                            if (i < 100) complete = true;
                                            offset++;
                                        }
                                    }
                                })) {
                                    Priority = ThreadPriority.BelowNormal,
                                    IsBackground = true
                                };
                                t.Start();
                            }
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex.Message);
                }
            }).Wait();
        }

        private void Chat_OnChatMessageReceived(object sender, ChatMessageContainerModel message) {
            foreach (ChatMessageModel m in message.chats) {
                ChatEmote[] emotes = new ChatEmote[0];
                string[] badges = new string[1];
                badges[0] = "http://localhost:8080/TrovoLogo.png";
                doChatMessage(m.nick_name, m.content, emotes, badges, DEFAULT_COLOR, "TROVO:"+message.eid);

                if (m.type == ChatMessageTypeEnum.FollowAlert) {
                    Logger.Trace("TrovoFollowAlert: " + m.content);
                    doFollow(m.nick_name);
                } else if (m.type == ChatMessageTypeEnum.SubscriptionAlert) {
                    Logger.Trace("TrovoSubAlert: " + m.content);
                    doSubscribe(m.nick_name, false, false, -1);
                }
            }
        }

        private long lastViewCount = 0;
        private DateTime lastUpdate = new DateTime(0);

        public override long getViewerCount() {
            TimeSpan ts = DateTime.Now - lastUpdate;
            if ((ts.TotalSeconds > 30) && (connection != null)) {
                lastUpdate = DateTime.Now;
                // Only call update every 30s (since needs API call, not continually tracked
                PrivateChannelModel channel = connection.Channels.GetCurrentChannel().Result;
                long viewCount = (channel == null) ? 0 : channel.current_viewers;
                Logger.Trace("Detected <" + viewCount + "> Trovo viewers");
                lastViewCount = viewCount;
            }
            return lastViewCount;
        }

        public override void updateCategory(string category) {
            if (connection == null) return;
            PrivateChannelModel channel = connection.Channels.GetCurrentChannel().Result;
            channel.category_name = category;
        }

        public override void updateTitle(string title) {
            if (connection == null) return;
            PrivateChannelModel channel = connection.Channels.GetCurrentChannel().Result;
            channel.live_title = title;
        }
    }
}
