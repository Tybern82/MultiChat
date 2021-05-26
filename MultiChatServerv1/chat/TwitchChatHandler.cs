﻿#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StreamingClient.Base.Util;
using Twitch.Base;
using Twitch.Base.Clients;
using Twitch.Base.Models.Clients.Chat;
using Twitch.Base.Models.Clients.PubSub;
using Twitch.Base.Models.Clients.PubSub.Messages;
using Twitch.Base.Models.NewAPI.Users;

namespace MultiChatServer.chat {
    public class TwitchChatHandler : ChatHandler {
        protected static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private const string clientID = "xm067k6ffrsvt8jjngyc9qnaelt7oo";
        private const string clientSecret = "jtzezlc6iuc18vh9dktywdgdgtu44b";

        private static readonly List<OAuthClientScopeEnum> scopes = new List<OAuthClientScopeEnum>() {
            OAuthClientScopeEnum.channel_commercial,
            OAuthClientScopeEnum.channel_editor,
            OAuthClientScopeEnum.channel_read,
            OAuthClientScopeEnum.channel_subscriptions,
            OAuthClientScopeEnum.user_subscriptions,

            OAuthClientScopeEnum.user_read,

            OAuthClientScopeEnum.bits__read,
            OAuthClientScopeEnum.channel__moderate,
            OAuthClientScopeEnum.channel__read__redemptions,
            OAuthClientScopeEnum.chat__edit,
            OAuthClientScopeEnum.chat__read,
            OAuthClientScopeEnum.user__edit,
            OAuthClientScopeEnum.whispers__read,
            OAuthClientScopeEnum.whispers__edit,
        };

        private static TwitchConnection? twitchAPI;
        private static PubSubClient? pubSub;
        private static UserModel? user;
        private static ChatClient? chat;
        private static SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private static List<string> currentUserList = new List<string>();
        private string TwitchName { get; set; }

        public TwitchChatHandler(string twitchName, ChatServer server) : base(server) {
            this.TwitchName = twitchName;

            Task.Run(async () => {
                try {

                    using (StreamWriter writer = new StreamWriter(File.Open("Packets.txt", FileMode.Create))) {
                        await writer.FlushAsync();
                    }
                    Logger.Info("Connecting to Twitch <" + twitchName + ">");

                    twitchAPI = await TwitchConnection.ConnectViaLocalhostOAuthBrowser(clientID, clientSecret, scopes);
                    if (twitchAPI != null) {
                        Logger.Trace("Twitch connection successful!");

                        user = await twitchAPI.NewAPI.Users.GetCurrentUser();
                        if (user != null) {
                            Logger.Trace("Logged in as: " + user.display_name);

                            Logger.Trace("Connecting to Chat...");

                            pubSub = new PubSubClient(twitchAPI);

                            pubSub.OnDisconnectOccurred += PubSub_OnDisconnectOccurred;
                            pubSub.OnSentOccurred += PubSub_OnSentOccurred;
                            pubSub.OnReconnectReceived += PubSub_OnReconnectReceived;
                            pubSub.OnResponseReceived += PubSub_OnResponseReceived;
                            pubSub.OnMessageReceived += PubSub_OnMessageReceived;
                            pubSub.OnWhisperReceived += PubSub_OnWhisperReceived;
                            pubSub.OnPongReceived += PubSub_OnPongReceived;

                            await pubSub.Connect();

                            await Task.Delay(1000);

                            List<PubSubListenTopicModel> topics = new List<PubSubListenTopicModel>();
                            foreach (PubSubTopicsEnum topic in EnumHelper.GetEnumList<PubSubTopicsEnum>()) {
                                topics.Add(new PubSubListenTopicModel(topic, user.id));
                            }

                            await pubSub.Listen(topics);
                            await Task.Delay(1000);
                            await pubSub.Ping();

                            chat = new ChatClient(twitchAPI);

                            chat.OnDisconnectOccurred += Chat_OnDisconnectOccurred;
                            chat.OnSentOccurred += Chat_OnSentOccurred;
                            chat.OnPacketReceived += Chat_OnPacketReceived;

                            chat.OnPingReceived += Chat_OnPingReceived;
                            chat.OnGlobalUserStateReceived += Chat_OnGlobalUserStateReceived;
                            chat.OnUserListReceived += Chat_OnUserListReceived;
                            chat.OnUserJoinReceived += Chat_OnUserJoinReceived;
                            chat.OnUserLeaveReceived += Chat_OnUserLeaveReceived;
                            chat.OnMessageReceived += Chat_OnMessageReceived;
                            chat.OnUserStateReceived += Chat_OnUserStateReceived;
                            chat.OnUserNoticeReceived += Chat_OnUserNoticeReceived;

                            await chat.Connect();

                            await Task.Delay(1000);

                            await chat.AddCommandsCapability();
                            await chat.AddTagsCapability();
                            await chat.AddMembershipCapability();

                            await Task.Delay(1000);

                            await chat.Join(user);

                            await Task.Delay(2000);

                            isConnected = true;
                            Logger.Trace(string.Format("There are {0} users currently in chat", currentUserList.Count()));
                        }
                    }
                } catch (Exception ex) {
                    Logger.Trace(ex.ToString());
                }
            }).Wait();
        }

        private static void PubSub_OnDisconnectOccurred(object sender, System.Net.WebSockets.WebSocketCloseStatus e) {
            Logger.Trace("DISCONNECTED");
        }

        private static void PubSub_OnSentOccurred(object sender, string packet) {
            Logger.Trace("SEND: " + packet);
        }

        private static void PubSub_OnReconnectReceived(object sender, System.EventArgs e) {
            Logger.Trace("RECONNECT");
        }

        private static void PubSub_OnResponseReceived(object sender, PubSubResponsePacketModel packet) {
            Logger.Trace("RESPONSE: " + packet.error);
        }

        private static void PubSub_OnMessageReceived(object sender, PubSubMessagePacketModel packet) {
            Logger.Trace(string.Format("MESSAGE: {0} {1} ", packet.type, packet.message));
            if (packet.type == "MESSAGE") {
                if (packet.topicType == PubSubTopicsEnum.ChannelSubscriptionsV1) {
                    
                }
            }
        }

        private static void PubSub_OnWhisperReceived(object sender, PubSubWhisperEventModel whisper) {
            Logger.Trace("WHISPER: " + whisper.body);
        }

        private static void PubSub_OnPongReceived(object sender, System.EventArgs e) {
            Logger.Trace("PONG");
            Task.Run(async () => {
                await Task.Delay(1000 * 60 * 3);
                if (pubSub != null) await pubSub.Ping();
            });
        }

        private static void Chat_OnGlobalUserStateReceived(object sender, ChatGlobalUserStatePacketModel packet) {
            Logger.Trace(string.Format("Connected as: {0} {1}", packet.UserID, packet.UserDisplayName));
        }

        private static void Chat_OnUserListReceived(object sender, ChatUsersListPacketModel packet) {
            currentUserList.AddRange(packet.UserLogins);
        }

        private static void Chat_OnUserJoinReceived(object sender, ChatUserJoinPacketModel packet) {
            currentUserList.Add(packet.UserLogin);
        }

        private static void Chat_OnUserLeaveReceived(object sender, ChatUserLeavePacketModel packet) {
            currentUserList.Remove(packet.UserLogin);
        }

        private static readonly string EMOTE_FORMAT = "{{ \"name\": {0}, \"link\": https://static-cdn.jtvnw.net/emoticons/v2/{1}/default/dark/1.0 }}";

        private void Chat_OnMessageReceived(object sender, ChatMessagePacketModel packet) {
            List<string> emotes = new List<string>();
            foreach (long e in packet.EmotesDictionary.Keys) {
                string setID = e.ToString();
                List<Tuple<int, int>> items = packet.EmotesDictionary[e];
                foreach (Tuple<int,int> i in items) {
                    string ident = packet.Message.Substring(i.Item1, i.Item2 - i.Item1 + 1);
                    emotes.Add(string.Format(EMOTE_FORMAT, ident, setID));
                }
            }

            string[] badges = new string[1];
            badges[0] = "http://localhost:8080/TwitchLogo.png";

            doChatMessage(packet.UserDisplayName, packet.Message, emotes.ToArray(), badges, packet.Color);
        }

        private static void Chat_OnUserStateReceived(object sender, ChatUserStatePacketModel packet) {
            Logger.Trace(string.Format("{0}: {1} {2}", packet.UserDisplayName, packet.UserBadges, packet.Color));
        }

        private static void Chat_OnUserNoticeReceived(object sender, ChatUserNoticePacketModel packet) {
            Logger.Trace(string.Format("USER NOTICE: {0} {1}", packet.UserDisplayName, packet.SystemMessage));
        }

        private static async void Chat_OnPingReceived(object sender, EventArgs e) {
            if (chat != null) await chat.Pong();
        }

        private static void Chat_OnDisconnectOccurred(object sender, System.Net.WebSockets.WebSocketCloseStatus e) {
            Logger.Trace("DISCONNECTED");
        }

        private static void Chat_OnSentOccurred(object sender, string packet) {
            Logger.Trace("SEND: " + packet);
        }

        private static async void Chat_OnPacketReceived(object sender, ChatRawPacketModel packet) {
            if (!packet.Command.Equals("PING") && !packet.Command.Equals(ChatMessagePacketModel.CommandID) && !packet.Command.Equals(ChatUserJoinPacketModel.CommandID)
                 && !packet.Command.Equals(ChatUserLeavePacketModel.CommandID)) {
                Logger.Trace("PACKET: " + packet.Command);

                await semaphore.WaitAndRelease(async () => {
                    using (StreamWriter writer = new StreamWriter(File.Open("Packets.txt", FileMode.Append))) {
                        await writer.WriteLineAsync(JSONSerializerHelper.SerializeToString(packet));
                        await writer.WriteLineAsync();
                        await writer.FlushAsync();
                    }
                });
            }
        }

        public override long getViewerCount() {
            return currentUserList.Count;
        }
    }
}
