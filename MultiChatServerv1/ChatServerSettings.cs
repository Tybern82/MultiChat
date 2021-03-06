#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrimeAPI.com.brimelive.api;
using Newtonsoft.Json.Linq;

namespace MultiChatServer {

    /// <summary>
    /// Settings object to control ChatServer instance. Includes which servers to start, as well as stores login names, etc.
    /// Class also contains list of 'viewers' to ignore from Twitch chat
    /// </summary>
    public class ChatServerSettings {

        /// <summary>
        /// Load settings from the given JSON data
        /// </summary>
        /// <param name="jsonData">data object containing settings to load</param>
        public ChatServerSettings(JToken jsonData) : this() {
            string? curr = jsonData.Value<string>("brimeName");
            BrimeName = curr ?? "";
            curr = jsonData.Value<string>("brimeChannelID");
            BrimeChannelID = curr ?? "";

            ConnectTwitch = jsonData.Value<bool>("connectTwitch");
            ConnectTrovo = jsonData.Value<bool>("connectTrovo");
            ConnectYouTube = jsonData.Value<bool>("connectYouTube");

            ShowLog = jsonData.Value<bool>("showLog");

            JArray? names = jsonData.Value<JArray>("ignoreNames");
            if (names != null) {
                IgnoreChatNames = new List<string>(names.Count);
                foreach (JToken item in names) {
                    curr = item.Value<string>();
                    if (curr != null) IgnoreChatNames.Add(curr);
                }
            }

            curr = jsonData.Value<string>("clientID");
            // NOTE: Yes, this is a redundant check for null on curr, however this is included
            //       since the compiler is unable to identify that curr is not-Null from just
            //       the method call (lacking annotations in this .Net runtime.
            if (!((curr == null) || string.IsNullOrWhiteSpace(curr))) {
                BrimeAPI.com.brimelive.api.BrimeAPI.ClientID = curr;
            }
        }

        /// <summary>
        /// Create a default set of settings.
        /// </summary>
        public ChatServerSettings() {
            BrimeName = "";
            BrimeChannelID = "";
            ConnectTwitch = true;
            ConnectTrovo = false;
            ConnectYouTube = false;
            ShowLog = false;
            IgnoreChatNames = new List<string>();
            // setup some defaults
            IgnoreChatNames.AddRange(new string[] {
                    // Add some recommended defaults
                    "streamelements", "streamlabs", "nightbot", "commanderroot", "soundalerts", "pretzelrocks", "restreambot"
                });
        }

        /// <summary>
        /// Login name to connect to on Brime (will trigger Channel ID lookup once Client-ID available)
        /// </summary>
        public string BrimeName { get; set; }

        /// <summary>
        /// Channel ID to connect to on Brime (should be preferred once new chat system implemented)
        /// </summary>
        public string BrimeChannelID { get; set; }

        /// <summary>
        /// Identify whether to connect to Twitch stream
        /// </summary>
        public bool ConnectTwitch { get; set; }

        /// <summary>
        /// Identify whether to connect to Trovo stream
        /// </summary>
        public bool ConnectTrovo { get; set; }

        /// <summary>
        /// Identify whether to connect to YouTube stream
        /// </summary>
        public bool ConnectYouTube { get; set; }

        /// <summary>
        /// Identify whether to show the log window
        /// </summary>
        public bool ShowLog { get; set; }

        /// <summary>
        /// List of names to ignore as "bot" accounts when counting Viewer numbers (Twitch only)
        /// </summary>
        public List<string> IgnoreChatNames { get; private set; }

        /// <summary>
        /// Convert settings back to JSON object to use when saving to settings file.
        /// </summary>
        /// <returns>JSON entity containing curent settings</returns>
        public string ToJSON() {
            StringBuilder _result = new StringBuilder();
            _result.Append("{\n")
                .Append(BrimeName.toJSON("brimeName")).Append(", \n")
                .Append(BrimeChannelID.toJSON("brimeChannelID")).Append(", \n")
                .Append(ConnectTwitch.toJSON("connectTwitch")).Append(", \n")
                .Append(ConnectTrovo.toJSON("connectTrovo")).Append(", \n")
                .Append(ConnectYouTube.toJSON("connectYouTube")).Append(", \n")
                .Append(ShowLog.toJSON("showLog")).Append(", \n")
                .Append(IgnoreChatNames.toJSON<string>("ignoreNames")).Append(", \n")
                .Append(BrimeAPI.com.brimelive.api.BrimeAPI.ClientID.toJSON("clientID"))
                .Append("}");
            return _result.ToString();
        }
    }
}