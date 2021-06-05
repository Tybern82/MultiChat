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
            BrimeName = (curr != null) ? curr : "";
            curr = jsonData.Value<string>("brimeChannelID");
            BrimeChannelID = (curr != null) ? curr : "";

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


        private string JSON_FORMAT = "{{ \n" +
            "    \"brimeName\": {0}, \n" +
            "    \"brimeChannelID\": {1}, \n" +
            "    \"connectTwitch\": {2}, \n" +
            "    \"connectTrovo\": {3}, \n" +
            "    \"connectYouTube\": {4}, \n" +
            "    \"showLog\": {5}, \n" +
            "    \"ignoreNames\": {6} \n" +
            "}}";

        /// <summary>
        /// Convert settings back to JSON object to use when saving to settings file.
        /// </summary>
        /// <returns>JSON entity containing curent settings</returns>
        public string ToJSON() {
            return string.Format(JSON_FORMAT,
                BrimeName.ToJSONString(),
                BrimeChannelID.ToJSONString(),
                ConnectTwitch.ToJSONString(),
                ConnectTrovo.ToJSONString(),
                ConnectYouTube.ToJSONString(),
                ShowLog.ToJSONString(),
                IgnoreChatNames.ToJSONString());
        }
    }
}