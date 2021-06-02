#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using YouTube.Base;
using YouTube.Base.Clients;

namespace MultiChatServer.chat {
    public class YoutubeChatHandler : ChatHandler {
        protected static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static string clientID = "884596410562-pcrl1fn8ov0npj7fhjl086ffmud7r5j6.apps.googleusercontent.com";
        public static string clientSecret = "QBkxNmPNIvWatRvOIfRYrXlc";

        public static readonly List<OAuthClientScopeEnum> scopes = new List<OAuthClientScopeEnum>()
        {
            OAuthClientScopeEnum.ChannelMemberships,
            OAuthClientScopeEnum.ManageAccount,
            OAuthClientScopeEnum.ManageData,
            OAuthClientScopeEnum.ManagePartner,
            OAuthClientScopeEnum.ManagePartnerAudit,
            OAuthClientScopeEnum.ManageVideos,
            OAuthClientScopeEnum.ReadOnlyAccount,
            OAuthClientScopeEnum.ViewAnalytics,
            OAuthClientScopeEnum.ViewMonetaryAnalytics
        };

        public YoutubeChatHandler(ChatServer server) : base(server) {
            Task.Run(async () => {
                try {
                    Logger.Trace("Initializing YouTube connection");

                    YouTubeConnection connection = await YouTubeConnection.ConnectViaLocalhostOAuthBrowser(clientID, clientSecret, scopes);
                    if (connection != null) {
                        Channel channel = await connection.Channels.GetMyChannel();
                        if (channel != null) {
                            Logger.Trace("Connection successful. Logged in as: " + channel.Snippet.Title);
                            server.YoutubeName.Invoke(channel.Snippet.Title);
                            Logger.Trace("Connecting chat client!");

                            ChatClient client = new ChatClient(connection);
                            client.OnMessagesReceived += Client_OnMessagesReceived;
                            if (await client.Connect()) {
                                Logger.Info("Live chat connection successful!");
                                isConnected = true;
                            } else {
                                Logger.Info("Failed to connect to live chat");
                            }
                        }
                    }
                } catch (Exception ex) {
                    Logger.Trace(ex.ToString());
                }
            });
        }

        public override long getViewerCount() {
            return 0;
        }

        public override void updateCategory(string category) {
            
        }

        public override void updateTitle(string title) {
            
        }

        private void Client_OnMessagesReceived(object sender, IEnumerable<LiveChatMessage> messages) {
            string[] badges = new string[1];
            badges[0] = "http://localhost:8080/YoutubeLogo.png";
            foreach (LiveChatMessage message in messages) {
                string msg = "";
                bool hasContent = (message.Snippet.HasDisplayContent != null) ? (bool)message.Snippet.HasDisplayContent : false;
                if (hasContent) msg = message.Snippet.DisplayMessage;
                doChatMessage(message.AuthorDetails.DisplayName,
                    msg,
                    new string[0],
                    badges,
                    DEFAULT_COLOR);
            }
        }
    }
}
