using System.Windows;
using System.Windows.Input;
using MultiChat.Helper;
using MultiChatServer;
using NLog;
using NLog.Config;
using NLog.Targets.Wrappers;
using IniParser;
using System.IO;
using System.Diagnostics;
using System;

namespace MultiChat {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public MainWindow() {
            InitializeComponent();
            logger.Trace("Initialized Main Window");
            try {
                FileIniDataParser parser = new FileIniDataParser();
                string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (appPath == null) appPath = "./";
                string fname = Path.Combine(appPath, "MultiChat.ini");
                if (File.Exists(fname)) {
                    IniParser.Model.IniData data = parser.ReadFile(fname);
                    string name = data["MultiChat"]["BrimeName"];
                    string id = data["MultiChat"]["BrimeChannelID"];
                    bool connTwitch = bool.Parse(data["MultiChat"]["ConnectTwitch"]);
                    bool connTrovo = bool.Parse(data["MultiChat"]["ConnectTrovo"]);
                    bool connYoutube = bool.Parse(data["MultiChat"]["ConnectYouTube"]);
                    bool showLog = bool.Parse(data["MultiChat"]["ShowLog"]);
                    txtChannelName.Text = name;
                    txtChannelID.Text = id;
                    chkTwitch.IsChecked = connTwitch;
                    chkTrovo.IsChecked = connTrovo;
                    chkYoutube.IsChecked = connYoutube;
                    chkShowLog.IsChecked = showLog;
                }
            } catch (Exception) {}
        }

        private ChatServer chatServer;
        private bool isConnect = true;

        private void btnConnect_Click(object sender, RoutedEventArgs e) {
            if (isConnect) {
                if (chkShowLog.IsChecked == true) {
                    if (LogManager.Configuration.FindTargetByName("RichTextAsync") == null) {
                        var target = new WpfRichTextBoxTarget {
                            Name = "RichText",
                            Layout =
                            "[${longdate:useUTC=false}] :: [${level:uppercase=true}] :: ${logger}:${callsite} :: ${message} ${exception:innerFormat=tostring:maxInnerExceptionLevel=10:separator=,:format=tostring}",
                            ControlName = "txtLogging",
                            FormName = "wndLogging",
                            AutoScroll = true,
                            MaxLines = 100,
                            UseDefaultRowColoringRules = true,
                            Width = 800,
                            Height = 450,
                        };
                        var asyncWrapper = new AsyncTargetWrapper { Name = "RichTextAsync", WrappedTarget = target };
                        LogManager.Configuration.AddTarget(asyncWrapper.Name, asyncWrapper);
                        LogManager.Configuration.LoggingRules.Insert(0, new LoggingRule("*", LogLevel.FromString("Trace"), asyncWrapper));
                        LogManager.ReconfigExistingLoggers();
                    }
                }
                this.Cursor = Cursors.Wait;
                chatServer = new ChatServer();
                if ((bool)chkTwitch.IsChecked) {
                    lblTwitch.Content = "Connecting...";
                    chatServer.TwitchName = (name) => {
                        wndMain.Dispatcher.Invoke(() => {
                            lblTwitch.Content = "Connected as: " + name;
                        });
                    };
                }
                if ((bool)chkTrovo.IsChecked) {
                    lblTrovo.Content = "Connecting...";
                    chatServer.TrovoName = (name) => {
                        wndMain.Dispatcher.Invoke(() => {
                            lblTrovo.Content = "Connected as: " + name;
                        });
                    };
                }
                if ((bool)chkYoutube.IsChecked) {
                    lblYoutube.Content = "Connecting...";
                    chatServer.YoutubeName = (name) => {
                        wndMain.Dispatcher.Invoke(() => {
                            lblYoutube.Content = "Connected as: " + name;
                        });
                    };
                }
                chatServer.Start(
                    txtChannelName.Text,
                    (bool)chkTwitch.IsChecked,
                    (bool)chkTrovo.IsChecked,
                    (bool)chkYoutube.IsChecked
                    );
                grdBrime.IsEnabled = false;
                grdTwitch.IsEnabled = false;
                grdTrovo.IsEnabled = false;
                grdYoutube.IsEnabled = false;
                chkShowLog.IsEnabled = false;
                btnConnect.Content = "Disconnect";
                isConnect = false;
                this.Cursor = Cursors.Arrow;
            } else {
                this.Cursor = Cursors.Wait;
                if (chatServer != null) chatServer.RunServer = false;
                if (chkShowLog.IsChecked == true) {
                    bool found = false;
                    for (int x = 0; x < Application.Current.Windows.Count && !found; x++) {
                        if (Application.Current.Windows[x].Name == "wndLogging") {
                            found = true;
                            Application.Current.Windows[x].Close();
                        }
                    }
                    LogManager.Configuration.RemoveTarget("RichTextAsync");
                    LogManager.ReconfigExistingLoggers();
                }
                chatServer = null;
                grdBrime.IsEnabled = true;
                grdTwitch.IsEnabled = true;
                grdTrovo.IsEnabled = true;
                grdYoutube.IsEnabled = true;
                chkShowLog.IsEnabled = true;
                btnConnect.Content = "Connect";
                isConnect = true;
                lblTwitch.Content = "Disconnected";
                lblTrovo.Content = "Disconnected";
                lblYoutube.Content = "Disconnected";
                this.Cursor = Cursors.Arrow;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) {
            if (chatServer != null) chatServer.RunServer = false;

            try {
                FileIniDataParser parser = new FileIniDataParser();
                string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (appPath == null) appPath = "./";
                string fname = Path.Combine(appPath, "MultiChat.ini");
                IniParser.Model.IniData data = new IniParser.Model.IniData();
                data["MultiChat"]["BrimeName"] = txtChannelName.Text;
                data["MultiChat"]["BrimeChannelID"] = txtChannelID.Text;
                data["MultiChat"]["ConnectTwitch"] = chkTwitch.IsChecked.ToString();
                data["MultiChat"]["ConnectTrovo"] = chkTrovo.IsChecked.ToString();
                data["MultiChat"]["ConnectYouTube"] = chkYoutube.IsChecked.ToString();
                data["MultiChat"]["ShowLog"] = chkShowLog.IsChecked.ToString();
                parser.WriteFile(fname, data, System.Text.Encoding.UTF8);
            } catch (Exception) {}

            wndMain.Close();
            Application.Current.Shutdown();
        }

        // private LogWindow wndLogging;

        private void wndMain_Loaded(object sender, RoutedEventArgs e) {
            /*
            Dispatcher.Invoke(() => {
                if (LogManager.Configuration.FindTargetByName("RichTextAsync") == null) {
                    var target = new WpfRichTextBoxTarget {
                        Name = "RichText",
                        Layout =
                        "[${longdate:useUTC=false}] :: [${level:uppercase=true}] :: ${logger}:${callsite} :: ${message} ${exception:innerFormat=tostring:maxInnerExceptionLevel=10:separator=,:format=tostring}",
                        ControlName = "txtLogging",
                        FormName = "wndLogging",
                        AutoScroll = true,
                        MaxLines = 100,
                        UseDefaultRowColoringRules = true,
                        Width = 800,
                        Height = 450,
                    };
                    var asyncWrapper = new AsyncTargetWrapper { Name = "RichTextAsync", WrappedTarget = target };
                    LogManager.Configuration.AddTarget(asyncWrapper.Name, asyncWrapper);
                    LogManager.Configuration.LoggingRules.Insert(0, new LoggingRule("*", LogLevel.FromString("Trace"), asyncWrapper));
                    LogManager.ReconfigExistingLoggers();
                }
            });
            */
        }
    }
}
