using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrimeAPI.com.brimelive.api.channels;
using BrimeAPI.com.brimelive.api.streams;
using BrimeAPI.com.brimelive.api.users;
using Newtonsoft.Json.Linq;

namespace BrimeAPI.Test {
    class Program {
        /// <summary>Class Logger instance</summary>
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        static void Main(string[] args) {
            /* Requires Special Access 
            UserClipsRequest;
            UserFollowingRequest;
            UserRequest;
            ChannelSubscriptionRequest
            */
            string curr;
            try {
                string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (appPath == null) appPath = "./";
                string fname = Path.Combine(appPath, "MultiChat.json");
                if (File.Exists(fname)) {
                    // Load JSON data
                    JObject jsonData = JObject.Parse(File.ReadAllText(fname));

                    curr = jsonData.Value<string>("clientID");
                    if (!string.IsNullOrWhiteSpace(curr))
                        BrimeAPI.com.brimelive.api.BrimeAPI.ClientID = curr;
                }
            } catch (Exception) {}

            string channelName = "geeken";

            Logger.Info("Checking TotalUsersRequest...");
            try {
                TotalUsersRequest req = new TotalUsersRequest();
                int totalUsers = req.getResponse();
                Logger.Trace("Identified " + totalUsers + " total registered users.");
            } catch (Exception e) {
                Logger.Error(e.ToString());
            }

            Logger.Info("Checking ChannelExistsRequest");
            try {
                ChannelExistsRequest req = new ChannelExistsRequest(channelName);
                bool hasChannel = req.getResponse();
                Logger.Trace("Channel <" + channelName + "> " + (hasChannel ? "exists" : "does not exist"));
            } catch (Exception e) {
                Logger.Error(e.ToString());
            }

            Logger.Info("Checking ChannelRequest");
            try {
                ChannelRequest req = new ChannelRequest(channelName);
                BrimeChannel channel = req.getResponse();
                Logger.Trace("Identified channel " + channelName);
                Logger.Trace(channel.ToString());
            } catch (Exception e) {
                Logger.Error(e.ToString());
            }

            Logger.Info("Checking LiveStreamsRequest");
            try {
                LiveStreamsRequest req = new LiveStreamsRequest();
                List<BrimeStream> live = req.getResponse();
                Logger.Trace("Detected " + live.Count + " live streams:");
                foreach (BrimeStream s in live)
                    Logger.Trace(s.ToString());
            } catch (Exception e) {
                Logger.Error(e.ToString());
            }

            Logger.Info("Checks complete");

            Console.ReadLine().Trim();
        }
    }
}
