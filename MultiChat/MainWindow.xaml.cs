#nullable enable

using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using MultiChat.Helper;
using MultiChatServer;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Config;
using NLog.Targets.Wrappers;

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
                string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (appPath == null) appPath = "./";
                string fname = Path.Combine(appPath, "MultiChat.json");
                if (File.Exists(fname)) {
                    // Load JSON data
                    JObject jsonData = JObject.Parse(File.ReadAllText(fname));
                    serverSettings = new ChatServerSettings(jsonData);
                } else {
                    serverSettings = new ChatServerSettings();
                }
            } catch (Exception) {
                serverSettings = new ChatServerSettings();
            }
            txtChannelName.Text = serverSettings.BrimeName;
            txtChannelID.Text = serverSettings.BrimeChannelID;
            chkTwitch.IsChecked = serverSettings.ConnectTwitch;
            chkTrovo.IsChecked = serverSettings.ConnectTrovo;
            chkYoutube.IsChecked = serverSettings.ConnectYouTube;
            chkShowLog.IsChecked = serverSettings.ShowLog;
        }

        private ChatServer? chatServer;
        private ChatServerSettings serverSettings;
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
                if (chkTwitch.IsChecked == true) {
                    lblTwitch.Content = "Connecting...";
                    chatServer.TwitchName = (name) => {
                        wndMain.Dispatcher.Invoke(() => {
                            lblTwitch.Content = "Connected as: " + name;
                        });
                    };
                }
                if (chkTrovo.IsChecked == true) {
                    lblTrovo.Content = "Connecting...";
                    chatServer.TrovoName = (name) => {
                        wndMain.Dispatcher.Invoke(() => {
                            lblTrovo.Content = "Connected as: " + name;
                        });
                    };
                }
                if (chkYoutube.IsChecked == true) {
                    lblYoutube.Content = "Connecting...";
                    chatServer.YoutubeName = (name) => {
                        wndMain.Dispatcher.Invoke(() => {
                            lblYoutube.Content = "Connected as: " + name;
                        });
                    };
                }
                chatServer.Start(serverSettings);
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
                string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (appPath == null) appPath = "./";
                string fname = Path.Combine(appPath, "MultiChat.json");
                StreamWriter output = new StreamWriter(new FileStream(fname, FileMode.Create));
                output.WriteLine(serverSettings.ToJSON());
                output.Close();
            } catch (Exception) {}

            wndMain.Close();
            Application.Current.Shutdown();
        }

        private void txtChannelName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
            if (serverSettings != null) serverSettings.BrimeName = txtChannelName.Text;
        }

        private void txtChannelID_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
            if (serverSettings != null) serverSettings.BrimeChannelID = txtChannelID.Text;
        }

        private void chkTwitch_Checked(object sender, RoutedEventArgs e) {
            if (serverSettings != null) serverSettings.ConnectTwitch = (chkTwitch.IsChecked == true);
        }

        private void chkTrovo_Checked(object sender, RoutedEventArgs e) {
            if (serverSettings != null) serverSettings.ConnectTrovo = (chkTrovo.IsChecked == true);
        }

        private void chkYoutube_Checked(object sender, RoutedEventArgs e) {
            if (serverSettings != null) serverSettings.ConnectYouTube = (chkYoutube.IsChecked == true);
        }

        private void chkShowLog_Checked(object sender, RoutedEventArgs e) {
            if (serverSettings != null) serverSettings.ShowLog = (chkShowLog.IsChecked == true);
        }
    }
}
