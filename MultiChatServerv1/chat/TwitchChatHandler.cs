#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using StreamingClient.Base.Util;
using Twitch.Base;
using Twitch.Base.Clients;
using Twitch.Base.Models.Clients.Chat;
using Twitch.Base.Models.Clients.PubSub;
using Twitch.Base.Models.Clients.PubSub.Messages;
using Twitch.Base.Models.NewAPI.Games;
using Twitch.Base.Models.NewAPI.Streams;
using Twitch.Base.Models.NewAPI.Users;

namespace MultiChatServer.chat {
    public class TwitchChatHandler : ChatHandler {
        protected static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private const string clientID = "yodb4mx7oif8l4un9jyd7uw6qhz8gn"; // "xm067k6ffrsvt8jjngyc9qnaelt7oo";
        private const string clientSecret = "bisw82ht2igcviud9wm5xtaa123v8u"; // "jtzezlc6iuc18vh9dktywdgdgtu44b";

        private static readonly List<OAuthClientScopeEnum> scopes = new List<OAuthClientScopeEnum>() {
            // OAuthClientScopeEnum.channel_commercial,
            // OAuthClientScopeEnum.channel_editor,
            OAuthClientScopeEnum.channel_read,
            OAuthClientScopeEnum.channel_subscriptions,
            OAuthClientScopeEnum.user_subscriptions,

            // OAuthClientScopeEnum.user_read,

            OAuthClientScopeEnum.bits__read,
            // OAuthClientScopeEnum.channel__moderate,
            // OAuthClientScopeEnum.channel__read__redemptions,
            OAuthClientScopeEnum.chat__edit,
            OAuthClientScopeEnum.chat__read,
            // OAuthClientScopeEnum.user__edit,
            // OAuthClientScopeEnum.whispers__read,
            // OAuthClientScopeEnum.whispers__edit,
        };

        private static TwitchConnection? twitchAPI;
        private static PubSubClient? pubSub;
        private static UserModel? user;
        private static ChatClient? chat;
        private static SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private static List<string> currentUserList = new List<string>();
        // private string TwitchName { get; set; }
        private ChatServerSettings settings;

        public TwitchChatHandler(ChatServer server, ChatServerSettings settings) : base(server) {
            // this.TwitchName = twitchName;
            this.settings = settings;

            Task.Run(async () => {
                try {
                    Logger.Info("Connecting to Twitch...");

                    twitchAPI = await TwitchConnection.ConnectViaLocalhostOAuthBrowser(clientID, clientSecret, scopes);
                    if (twitchAPI != null) {
                        Logger.Info("Twitch connection successful!");

                        // UserModel chatUser = await twitchAPI.NewAPI.Users.GetUserByLogin(twitchName);
                        user = await twitchAPI.NewAPI.Users.GetCurrentUser();
                        if (user != null) {
                            Logger.Info("Logged in as: " + user.display_name);
                            Logger.Info("Connecting to Chat...");

                            pubSub = new PubSubClient(twitchAPI);

                            // pubSub.OnDisconnectOccurred += PubSub_OnDisconnectOccurred;
                            // pubSub.OnSentOccurred += PubSub_OnSentOccurred;
                            // pubSub.OnReconnectReceived += PubSub_OnReconnectReceived;
                            // pubSub.OnResponseReceived += PubSub_OnResponseReceived;
                            // pubSub.OnWhisperReceived += PubSub_OnWhisperReceived;
                            pubSub.OnPongReceived += PubSub_OnPongReceived;
                            pubSub.OnSubscribedReceived += PubSub_OnSubscribedReceived;
                            pubSub.OnSubscriptionsGiftedReceived += PubSub_OnSubscriptionsGiftedReceived;

                            await pubSub.Connect();

                            await Task.Delay(1000);

                            List<PubSubListenTopicModel> topics = new List<PubSubListenTopicModel>();
                            topics.Add(new PubSubListenTopicModel(PubSubTopicsEnum.ChannelSubscriptionsV1, user.id));
                            topics.Add(new PubSubListenTopicModel(PubSubTopicsEnum.ChannelBitsEventsV2, user.id));

                            /*
                            List<PubSubListenTopicModel> topics = new List<PubSubListenTopicModel>();
                            foreach (PubSubTopicsEnum topic in EnumHelper.GetEnumList<PubSubTopicsEnum>()) {
                                topics.Add(new PubSubListenTopicModel(topic, user.id));
                            }
                            */

                            await pubSub.Listen(topics);
                            await Task.Delay(1000);
                            await pubSub.Ping();

                            // Need to poll for changes to Follower list, since we can't receive the WebHook connection
                            // without requiring users to create SSL certificates, etc
                            Thread t = new Thread(new ThreadStart((() => {
                                // Record existing followers
                                HashSet<string> fIDs = new HashSet<string>();
                                Logger.Trace("Recording existing Twitch followers");
                                foreach (var follower in twitchAPI.NewAPI.Users.GetFollows(null, user.id, int.MaxValue).Result) {
                                    fIDs.Add(follower.from_id);
                                }
                                while (server.RunServer) {
                                    Thread.Sleep(30000);
                                    // Get updated list, and compare with existing
                                    Logger.Trace("Detecting new Twitch followers");
                                    foreach (var follower in twitchAPI.NewAPI.Users.GetFollows(null, user.id, int.MaxValue).Result) {
                                        if (!fIDs.Contains(follower.from_id)) {
                                            Logger.Trace("Found new Twitch follower: " + follower.from_name);
                                            fIDs.Add(follower.from_id);
                                            doFollow(follower.from_name);
                                        }
                                    }
                                }
                            })));
                            t.Priority = ThreadPriority.BelowNormal;
                            t.IsBackground = true;
                            t.Start();
                            
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
                            server.TwitchName.Invoke(user.display_name);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Trace(ex.ToString());
                }
            }).Wait();
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
            Logger.Trace("Initial Twitch chat: " + BrimeAPI.com.brimelive.api.JSONUtil.ToJSONString(packet.UserLogins));
            currentUserList.AddRange(packet.UserLogins);
        }

        private static void Chat_OnUserJoinReceived(object sender, ChatUserJoinPacketModel packet) {
            Logger.Trace(packet.UserLogin + " entered Twitch chat");
            currentUserList.Add(packet.UserLogin);
        }

        private static void Chat_OnUserLeaveReceived(object sender, ChatUserLeavePacketModel packet) {
            Logger.Trace(packet.UserLogin + " left Twitch chat");
            currentUserList.Remove(packet.UserLogin);
        }

        private static readonly string EMOTE_FORMAT = "{{ \"name\": \"{0}\", \"link\": \"https://static-cdn.jtvnw.net/emoticons/v2/{1}/default/dark/1.0\" }}";

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
            
            doChatMessage((string.IsNullOrWhiteSpace(packet.UserDisplayName) ? packet.UserLogin : packet.UserDisplayName), packet.Message, emotes.ToArray(), badges, packet.Color);
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

        private void Chat_OnDisconnectOccurred(object sender, System.Net.WebSockets.WebSocketCloseStatus e) {
            Logger.Trace("DISCONNECTED");
            isConnected = false;
        }

        private static void Chat_OnSentOccurred(object sender, string packet) {
            Logger.Trace("SEND: " + packet);
        }

        private static void Chat_OnPacketReceived(object sender, ChatRawPacketModel packet) {
            if (!packet.Command.Equals("PING") && !packet.Command.Equals(ChatMessagePacketModel.CommandID) && !packet.Command.Equals(ChatUserJoinPacketModel.CommandID)
                 && !packet.Command.Equals(ChatUserLeavePacketModel.CommandID)) {
                Logger.Trace("PACKET: " + packet.Command);
            }
        }

        public override long getViewerCount() {
            // Need '-1' to remove the current "viewer" where this app connects to chat
            int chattersToRemove = 1; // start with broadcaster
            foreach (string s in settings.IgnoreChatNames) {
                if (currentUserList.Contains(s)) chattersToRemove++;
            }
            int result = currentUserList.Count - chattersToRemove;
            if (result < 0) result = 0;
            return result;
        }

        public override void updateCategory(string category) {
            if ((twitchAPI == null) || (user == null)) return;
            var streams = twitchAPI.NewAPI.Streams.GetStreamsByUserIDs(new string[] { user.id }).Result;
            IEnumerable<GameModel> games = twitchAPI.NewAPI.Games.GetGamesByName(category).Result;
            if ((games == null) || (games.Count() == 0)) return;  // no valid game found
            foreach (var s in streams) {
                s.game_id = games.First().id;
            }
        }

        public override void updateTitle(string title) {
            if ((twitchAPI == null) || (user == null)) return;
            var streams = twitchAPI.NewAPI.Streams.GetStreamsByUserIDs(new string[] { user.id }).Result;
            foreach (StreamModel s in streams) {
                s.title = title;
            }
        }

        private void PubSub_OnSubscriptionsGiftedReceived(object sender, PubSubSubscriptionsGiftEventModel e) {
            doSubscribe(e.display_name, e.cumulative_months > 1, true, e.cumulative_months);
        }

        private void PubSub_OnSubscribedReceived(object sender, PubSubSubscriptionsEventModel e) {
            doSubscribe(e.display_name, e.cumulative_months > 1, false, e.cumulative_months);
        }
    }
}


/* Sub Message
"message": {
      "user_name": "tww2",
      "display_name": "TWW2",
      "channel_name": "mr_woodchuck",
      "user_id": "13405587",
      "channel_id": "89614178",
      "time": "2015-12-19T16:39:57-08:00",
      "sub_plan": "1000",
      "sub_plan_name": "Channel Subscription (mr_woodchuck)",
      "cumulative_months": 9,
      "streak_months": 3,
      "context": "resub",
      "is_gift": false,
      "sub_message": {
        "message": "A Twitch baby is born! KappaHD",
        "emotes": [
          {
            "start": 23,
            "end": 7,
            "id": 2867
          }
        ]
      }
    }
*/