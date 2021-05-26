#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using MultiChatServer;

namespace MultiChatConsole {
    class Program {
        static void Main(string[] args) {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);
        }

        private static void RunOptions(Options opts) {
            ChatServer chatServer = new ChatServer();

            SetNlogLogLevel(opts.Verbose ? NLog.LogLevel.Trace : NLog.LogLevel.Info);
            chatServer.Start(opts.BrimeName, opts.TwitchName, opts.TrovoName, opts.AddYoutube);

            Console.WriteLine("Open http://localhost:8080/ to view combined chat");
            Console.WriteLine("Open http://localhost:8080/Notifications.html to view notification information");
            Console.WriteLine();
            Console.WriteLine("Options: ?darkmode = use darkmode display (black background)");
            Console.WriteLine("         ?nofade   = ignore message fading (disconnect message will still auto-fade)");
            Console.WriteLine();
            Console.WriteLine("Type QUIT to close (case-insensitive)");
            while (!Console.ReadLine().Trim().Equals("quit", StringComparison.InvariantCultureIgnoreCase)) ;
            chatServer.RunServer = false;
        }

        private static void SetNlogLogLevel(NLog.LogLevel level) {
            // Uncomment these to enable NLog logging. NLog exceptions are swallowed by default.
            ////NLog.Common.InternalLogger.LogFile = @"C:\Temp\nlog.debug.log";
            ////NLog.Common.InternalLogger.LogLevel = LogLevel.Debug;
            var target = NLog.LogManager.Configuration.FindTargetByName("logconsole");
            foreach (var rule in NLog.LogManager.Configuration.LoggingRules) {
                if (rule.Targets.Contains(target)) {
                    foreach (var i in NLog.LogLevel.AllLoggingLevels) {
                        if (i < level) {
                            rule.DisableLoggingForLevel(i);
                        } else {
                            rule.EnableLoggingForLevel(i);
                        }
                    }
                }
            }
            NLog.LogManager.ReconfigExistingLoggers();
        }

        private static void HandleParseError(IEnumerable<Error> obj) {
            NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
            log.Error("Unable to process command line arguments.");
            foreach (var i in obj)
                log.Error(i.ToString());
        }
    }
    class Options {

        [Option("brime", Required = false, HelpText = "Brime login name", Default = "")]
        public string BrimeName { get; set; } = "";

        [Option("trovo", Required = false, HelpText = "Trovo login name", Default = "")]
        public string TrovoName { get; set; } = "";

        [Option("twitch", Required = false, HelpText = "Twitch login name", Default = "")]
        public string TwitchName { get; set; } = "";

        [Option("youtube", Required = false, HelpText = "Activate YouTube connection", Default = false)]
        public bool AddYoutube { get; set; } = false;

        // Omitting long name, defaults to name of property, ie "--verbose"
        [Option(
          Default = false,
          HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }
    }
}
