#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MultiChatServer.chat;
using Newtonsoft.Json;
using WatsonWebsocket;

namespace MultiChatServer {
    public class ChatServer {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public HttpListener? listener;
        private WatsonWsServer? server;
        // private MergedChatbot? Chatbot;

        private bool _RunServer = false;
        public bool RunServer { 
            get {
                return _RunServer;
            }
            set {
                _RunServer = value;
                if (listener != null) listener.Stop();
            }
        }

        public bool IsConnected { get; private set; } = false;

        public async Task HandleIncomingConnections() {
            if (listener == null) return;

            while (RunServer) {
                try {
                    // Will wait here until we hear from a connection
                    HttpListenerContext ctx = await listener.GetContextAsync();

                    // Peel out the requests and response objects
                    HttpListenerRequest req = ctx.Request;
                    HttpListenerResponse resp = ctx.Response;

                    try {
                        if (req.HttpMethod == "GET") {
                            string path = req.RawUrl.Substring(1);
                            path = path.Split(new char[] { '?' }, StringSplitOptions.None)[0];
                            if (path.Equals("")) path = "ChatWindow.html";    // default page
                            string? appPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                            if (appPath == null) appPath = "./";
                            appPath = Path.Combine(appPath, "web");
                            string fname = Path.Combine(appPath, path);
                            Logger.Trace(() => { return string.Format("Requested File: <{0}>", path); });
                            if (!File.Exists(fname)) {
                                Logger.Warn(() => { return "File not found: <" + fname + ">"; });
                                SendErrorResponse(resp, 404, string.Format("Requested file <{0}> not found.", path), path);
                            } else {
                                using var fileStream = File.OpenRead(fname); 
                                resp.ContentType = MimeMapping.MimeUtility.GetMimeMapping(fname);
                                resp.ContentLength64 = (new FileInfo(fname)).Length;
                                fileStream.CopyTo(resp.OutputStream);
                            }
                        }
                    } catch (Exception e) {
                        Logger.Error(e.ToString());
                    }
                    resp.Close();
                } catch (HttpListenerException) {}
            }
            IsConnected = false;
            server?.Stop();
            Logger.Trace("Shutdown Chat Server");
        }

        private void SendErrorResponse(HttpListenerResponse response, int statusCode, string statusResponse, string reqName) {
            response.StatusCode = statusCode;
            if (statusCode != 404) {
                response.ContentLength64 = 0;
                response.StatusDescription = statusResponse;
            } else {
                byte[] data = Encoding.UTF8.GetBytes(String.Format(Error404, reqName));

                response.ContentType = "text/html";
                response.ContentEncoding = Encoding.UTF8;
                response.ContentLength64 = data.LongLength;

                response.OutputStream.WriteAsync(data, 0, data.Length).GetAwaiter().GetResult();
            }
            response.OutputStream.Close();
            Logger.Warn(() => { return string.Format("*** Sent error: {0} {1}", statusCode, statusResponse); });
        }

        private static readonly string Error404 = "<!DOCTYPE html>\n" +
            "<html lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\">\n" +
            "   <head>\n" +
            "      <meta charset=\"utf-8\" />\n" +
            "      <title>404: File Not Found</title>\n" +
            "   </head>\n" +
            "   <body>\n" +
            "      <h1>404: File Not Found</h1>\n" +
            "      <p>\n" +
            "         The requested file {0} is not available from this server. Please check you are using the correct\n" +
            "         method of accessing this service.\n" +
            "      </p>\n" +
            "      <p>\n" +
            "         Please use <a href=\"http://localhost:8080/\">http://localhost:8080/</a> or <a href=\"http://localhost:8080/ChatWindow.html\">http://localhost:8080/ChatWindow.html</a><br/>\n" +
            "         Alternatively, use <a href=\"http://localhost:8080/Notifications.html\">http://localhost:8080/Notifications.html</a> for notification messages.\n" +
            "      </p>\n" +
            "      <h2>Optional Parameters</h2>\n" +
            "      <ul>\n" +
            "         <li>?darkmode - use black background on page, rather than white</li>\n" +
            "         <li>?nofade - disable automatic fading of chat messages (useful for streamer chat display)</li>\n" +
            "      </ul>\n" +
            "   </body>\n" +
            "</html>\n";

        /*
        public void Start(string brimeName, bool hasTwitch, bool hasTrovo, bool hasYoutube) {
            if (RunServer) return;  // already running
            RunServer = true;
            Task.Run(() => {
                this.listener = new HttpListener();
                // TODO: Support changing Port - suggestion by Brime/Pimpek
                listener.Prefixes.Add("http://localhost:8080/");
                listener.Start();

                HandleIncomingConnections().GetAwaiter().GetResult();

                listener.Close();
            });

            Task.Run(() => {
                server = new WatsonWsServer("localhost", 8081, false);

                if (!string.IsNullOrWhiteSpace(brimeName)) handlers.Add(new BrimeChatHandler(brimeName, this));
                if (hasTwitch) handlers.Add(new TwitchChatHandler(this, settings));
                if (hasTrovo) handlers.Add(new TrovoChatHandler(this));
                if (hasYoutube) handlers.Add(new YoutubeChatHandler(this));
                server.ClientConnected += onClientConnected;
                server.ClientDisconnected += onClientDisconnected;
                IsConnected = true;
                // Chatbot = new MergedChatbot(brimeName, twitchName, server);
                server.Start();
            });
        }
        */

        public void Start(ChatServerSettings settings) {
            if (RunServer) return;  // already running
            RunServer = true;
            Task.Run(() => {
                this.listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:8080/");
                listener.Start();
                HandleIncomingConnections().GetAwaiter().GetResult();
                listener.Close();
            });
            Task.Run(() => {
                server = new WatsonWsServer("localhost", 8081, false);
                if (!string.IsNullOrWhiteSpace(settings.BrimeName)) handlers.Add(new BrimeChatHandler(this, settings));
                if (settings.ConnectTwitch) handlers.Add(new TwitchChatHandler(this, settings));
                if (settings.ConnectTrovo) handlers.Add(new TrovoChatHandler(this));
                if (settings.ConnectYouTube) handlers.Add(new YoutubeChatHandler(this));
                server.ClientConnected += onClientConnected;
                server.ClientDisconnected += onClientDisconnected;
                IsConnected = true;
                server.Start();
            });
        }

        public delegate void SetName(string name);

        public SetName TwitchName { get; set; } = (name) => { };
        public SetName TrovoName { get; set; } = (name) => { };
        public SetName YoutubeName { get; set; } = (name) => { };

        public void updateTitle(string title) {
            Logger.Trace("Updating stream titles: <" + title + ">");
            foreach (ChatHandler h in handlers) {
                if (h.isConnected) h.updateTitle(title);
            }
        }

        public void updateCategory(string category) {
            Logger.Trace("Updating stream category: <" + category + ">");
            foreach (ChatHandler h in handlers) {
                if (h.isConnected) h.updateCategory(category);
            }
        }

        private readonly List<ChatHandler> handlers = new List<ChatHandler>();
        public bool ForceViewUpdate { get; private set; }
        protected static readonly string VIEW_FORMAT = "{{\"type\": \"VIEWERCOUNT\", \"viewerCount\": {0} }}";
        private bool isRunningViewCount = false;

        public void onClientConnected(object? sender, ClientConnectedEventArgs args) {
            Logger.Trace(() => { return string.Format("Connecting client: <{0}>", args.IpPort); });
            lock (this) {
                if (!isRunningViewCount) {
                    isRunningViewCount = true;
                    Thread t = new Thread(new ThreadStart(() => {
                        long lastViewCount = 0;
                        int updateCount = 15;
                        int loopCount = 0;
                        while (true) {
                            try {
                                long viewCount = 0;
                                foreach (ChatHandler h in handlers) {
                                    // Make sure this connection has completed loading 
                                    if (h.isConnected) viewCount += h.getViewerCount();
                                }
                                if (ForceViewUpdate || (viewCount != lastViewCount)) {
                                    send(string.Format(VIEW_FORMAT, JsonConvert.ToString(viewCount)));
                                    lastViewCount = viewCount;
                                    ForceViewUpdate = false;
                                }
                                loopCount++;
                                loopCount %= updateCount;
                                Thread.Sleep(5000);
                            } catch (Exception e) {
                                Logger.Trace(e.ToString());
                            }
                        }
                    })) {
                        Priority = ThreadPriority.BelowNormal,
                        IsBackground = true
                    };
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

        private HashSet<string> ConnectedClients { get; set; } = new HashSet<string>();
        public void send(string jsonMessage) {
            byte[] data = Encoding.UTF8.GetBytes(jsonMessage);
            Logger.Trace(() => { return string.Format("Sending message: {0}", jsonMessage); });
            foreach (string ipPort in ConnectedClients) {
                Logger.Trace(() => { return string.Format("Sending to client: <{0}>", ipPort); });
                server?.SendAsync(ipPort, data, WebSocketMessageType.Text).GetAwaiter().GetResult();
            }
        }
    }
} 
