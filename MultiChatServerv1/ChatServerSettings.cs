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

        public ChatServerSettings(JToken jsonData) {
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
            } else {
                IgnoreChatNames = new List<string>();
                // setup some defaults
                IgnoreChatNames.AddRange(new string[] {
                    // Add some recommended defaults
                    "streamelements", "streamlabs", "nightbot", "commanderroot", "soundalerts", "pretzelrocks", "restreambot"
                });
            }
        }

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

        public string BrimeName { get; set; }
        public string BrimeChannelID { get; set; }
        public bool ConnectTwitch { get; set; }
        public bool ConnectTrovo { get; set; }
        public bool ConnectYouTube { get; set; }
        public bool ShowLog { get; set; }

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