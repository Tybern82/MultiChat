#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BrimeAPI.com.brimelive.api;
using BrimeAPI.com.brimelive.api.realtime;
using Newtonsoft.Json;
using TwitchAPI.twitchapi;
using TwitchAPI.twitchapi.auth;
using TwitchAPI.twitchapi.emotes;
using TwitchAPI.twitchapi.irc;
using TwitchAPI.twitchapi.streams;
using TwitchAPI.twitchapi.users;
using WatsonWebsocket;

namespace MultiChatServer {
    public class MergedChatbot : BrimeRealtimeListener, IRCListener {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public BrimeRealtimeAPI? BrimeAPI { get; private set; }
        public TwitchIRC? TwitchChat { get; private set; }
        public TwitchAPIUtil? TwitchAPI { get; private set; }

        private OAuthBaseRequest? oAuth;

        private WatsonWsServer Server { get; set; }

        private HashSet<string> ConnectedClients { get; set; } = new HashSet<string>();

        private string BrimeName { get; set; }
        private string TwitchName { get; set; }

        private bool isConnected = false;

        public MergedChatbot(string brimeName, string twitchName, WatsonWsServer server) {
            this.BrimeName = brimeName;
            this.TwitchName = twitchName;
            this.Server = server;

            if (!string.IsNullOrWhiteSpace(brimeName)) {
                BrimeAPI = new BrimeRealtimeAPI(brimeName);
                BrimeAPI.registerListener(this);
            }

            if (!string.IsNullOrWhiteSpace(twitchName)) {
                oAuth = new OAuthAuthorizationRequest();
                TwitchChat = new TwitchIRC(twitchName, oAuth);
                TwitchAPI = new TwitchAPIUtil(oAuth);
            }

            server.ClientConnected += onClientConnected;
            server.ClientDisconnected += onClientDisconnected;
            // server.MessageReceived += onMessageReceived; // message from Client to Server
        }

        public bool ForceViewUpdate { get; private set; }

        public void onClientConnected(object? sender, ClientConnectedEventArgs args) {
            Logger.Trace(() => { return string.Format("Connecting client: <{0}>", args.IpPort); });
            lock (this) {
                if (!isConnected) {
                    isConnected = true;
                    if (TwitchChat != null) {
                        Logger.Info("Connecting TwitchAPI <" + TwitchName + ">");
                        TwitchChat.ConnectSSL();
                        TwitchChat.joinChannel(TwitchName, this);
                    }
                    if (BrimeAPI != null) {
                        Logger.Info("Connecting BrimeAPI <" + BrimeName + ">");
                        BrimeAPI.connect();
                    }
                    Thread t = new Thread(new ThreadStart(() => {
                        int lastViewCount = 0;
                        int twitchViewCount = 0;
                        int updateCount = 15;
                        int loopCount = 0;
                        while (true) {
                            int viewCount = (BrimeAPI == null ? 0 : BrimeAPI.ViewCountTracker.ViewCount);
                            GetStreamsRequest req = new GetStreamsRequest();
                            req.UserLogin.Add(TwitchName);
                            /*
                             * Because we have no live-updated viewer count from Twitch, buffer the result of the 
                             * viewer count check and only trigger infrequently to avoid overloading the Twitch 
                             * service.
                             * Note: We also backoff and update more infrequently if we can't see an active stream
                             * on Twitch.
                             */
                            try {
                                if ((oAuth != null) && (TwitchAPI != null) && (ForceViewUpdate || (loopCount == 0))) {
                                    // Triggers every 15 loops (around 30s), or if Forced
                                    List<TwitchStream> streams = req.doRequest(oAuth);
                                    if (streams.Count > 0) {
                                        twitchViewCount = streams[0].ViewerCount; updateCount = 15;
                                        Logger.Trace(() => { return string.Format("Detected <{0}> Twitch viewers", streams[0].ViewerCount); });
                                    } else {
                                        twitchViewCount = 0; updateCount = 60;  // extend the update time to 60 loops (2m) if we're not seeing any active stream
                                        Logger.Trace("No streams returned in query");
                                    }
                                    loopCount = 0;
                                }
                            } catch (Exception) { }  // ignore any errors when trying to retrieve the viewer-count from Twitch
                            viewCount += twitchViewCount;
                            if (ForceViewUpdate || (viewCount != lastViewCount)) {
                                send(string.Format(VIEW_FORMAT, JsonConvert.ToString(viewCount)));
                                lastViewCount = viewCount;
                                ForceViewUpdate = false;
                            }
                            loopCount++;
                            loopCount = loopCount % updateCount;
                            Thread.Sleep(2000);
                        }
                    }));
                    t.IsBackground = true;
                    t.Start();
                }
            }
            ConnectedClients.Add(args.IpPort);
            ForceViewUpdate = true;
        }

        public void onClientDisconnected(object? sender, ClientDisconnectedEventArgs args) {
            Logger.Trace(() => { return string.Format("Disconnecting client: <{0}>", args.IpPort); });
            ConnectedClients.Remove(args.IpPort);
        }

        // Brime Messages
        public void onChat(BrimeChatMessage chatMessage) {
            string jsonMessage = EncodeBrimeMessage(chatMessage);
            send(jsonMessage);
        }

        // Twitch Messages
        public void onMessageReceived(UserDetails user, string message, string emotes) {
            string jsonMessage = EncodeTwitchMessage(user, message, emotes);
            send(jsonMessage);
        }

        public void send(string jsonMessage) {
            byte[] data = Encoding.UTF8.GetBytes(jsonMessage);
            Logger.Trace(() => { return string.Format("Sending message: {0}", jsonMessage); });
            foreach (string ipPort in ConnectedClients) {
                Logger.Trace(() => { return string.Format("Sending to client: <{0}>", ipPort); });
                Server.SendAsync(ipPort, data, WebSocketMessageType.Text).GetAwaiter().GetResult();
            }
        }

        public void onClose() { }

        public void onFollow(string username, string id) {
            send(string.Format(FOLLOW_FORMAT, JsonConvert.ToString(username)));
        }

        public void onFollow(string username) {
            onFollow(username, "");
        }

        public void onSubscribe(string username, string subID, bool isResub) {
            send(string.Format(SUB_FORMAT, JsonConvert.ToString(username), JsonConvert.ToString(isResub)));
        }

        public void onSubscribe(string username) {
            onSubscribe(username, "", false);
        }

        public void onJoin(string username) { }

        public void onLeave(string username) { }

        public void onOpen() { }

        public static readonly string CHAT_FORMAT = "{{\"type\": \"CHAT\", \"sender\": {{\"displayname\": {0}, \"color\": {1}, \"badges\": {2}}}, \"emotes\": {3}, \"message\": {4}, \"mstimeout\": {5} }}";
        public static readonly string FOLLOW_FORMAT = "{{\"type\": \"FOLLOW\", \"username\": {0} }}";
        public static readonly string SUB_FORMAT = "{{\"type\": \"SUBSCRIBE\", \"username\": {0}, \"isResub\": {1} }}";
        public static readonly string VIEW_FORMAT = "{{\"type\": \"VIEWERCOUNT\", \"viewerCount\": {0} }}";
        public int Timeout { get; set; } = 30000;

        private string EncodeBrimeMessage(BrimeChatMessage msg) {
            // msg.type = CHAT
            // msg.sender.displayname
            // msg.sender.color
            // msg.sender.badges []

            // msg.emotes { name, link }[]
            // msg.message
            // msg.mstimeout
            Logger.Trace(() => { return "Encode message: " + msg.ToString(); });
            // Prefix Brime messages with Brime logo to identify chat source
            string[] badges = new string[msg.Sender.Badges.Count + 1];
            badges[0] = "http://localhost:8080/BrimeLogo.png";
            msg.Sender.Badges.ToArray().CopyTo(badges, 1);
            try {
                return string.Format(CHAT_FORMAT,
                    JsonConvert.ToString(msg.Sender.DisplayName),   // msg.Sender.DisplayName
                    JsonConvert.ToString(msg.Sender.Color),         // msg.sender.color
                    JSONUtil.ToString(badges),                      // msg.Sender.Badges (array of img link)
                    JSONUtil.ToString(new string[0]),               // msg.Emotes
                    JsonConvert.ToString(msg.Message),
                    JsonConvert.ToString(Timeout)                   // default 30s timeout
                    );
            } catch (Exception e) {
                Logger.Error(e.ToString());
                return "";
            }
        }

        private string EncodeTwitchMessage(UserDetails user, string message, string emotes) {
            Dictionary<string, string> usedEmotes = new Dictionary<string, string>();
            if (TwitchAPI != null) {
                if (!string.IsNullOrWhiteSpace(emotes)) {
                    Logger.Trace("Detecting emotes from message");
                    string[] emoteSets = emotes.Split(new char[] { '/' });
                    foreach (string set in emoteSets) {
                        Match m = Regex.Match(set, "(?<setID>[0-9]*):(?<startIdx>[0-9]*)-(?<endIdx>[0-9]*)");
                        string setID = m.Groups["setID"].Value;
                        int startIdx = int.Parse(m.Groups["startIdx"].Value);
                        int endIdx = int.Parse(m.Groups["endIdx"].Value);

                        string ident = message.Substring(startIdx, endIdx - startIdx + 1);
                        if (!usedEmotes.ContainsKey(ident)) {
                            string url = TwitchAPI.GetEmoteURL(setID, ident);
                            Logger.Trace(() => { return string.Format("Loading emote <{0}> as <{1}>", ident, url); });
                            usedEmotes.Add(ident, url);
                        }
                    }
                    Logger.Trace("All emotes detected");
                }
            } else {
                Logger.Trace("No TwitchAPI interface loaded");
            }
            // Prefix Twitch messages with Twitch logo to identify chat source
            string[] badges = new string[1];
            badges[0] = "http://localhost:8080/TwitchLogo.png";
            // msg.Sender.Badges.ToArray().CopyTo(badges, 1);
            try {
                return string.Format(CHAT_FORMAT,
                    JsonConvert.ToString(user.DisplayName),
                    JsonConvert.ToString(""),
                    JSONUtil.ToString(badges),
                    GetEmotesString(usedEmotes),
                    JsonConvert.ToString(message),
                    JsonConvert.ToString(Timeout)
                    );
            } catch (Exception e) {
                Logger.Error(e.ToString());
                return "";
            }
        }

        private static readonly string EMOTE_FORMAT = "{{ \"name\": {0}, \"link\": {1} }}";

        private static string GetEmotesString(Dictionary<string, string> usedEmotes) {
            StringBuilder _result = new StringBuilder();
            _result.Append("[");
            bool isFirst = true;
            foreach (string k in usedEmotes.Keys) {
                if (!isFirst) _result.Append(", ");
                _result.Append(string.Format(EMOTE_FORMAT,
                    JsonConvert.ToString(k),
                    JsonConvert.ToString(usedEmotes[k])
                    ));
                isFirst = false;
            }
            _result.Append("]");
            return _result.ToString();
        }
    }
}
*/