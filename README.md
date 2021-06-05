# MultiChat
 Merged chat system for multi-streaming support. Provides both a GUI and console interface. Run MultiChat.exe for the GUI, or MultiChatConsole.exe for the console version.

    Chat Support: Brime, Twitch, Trovo. May support YouTube, however this has not been tested.
    Viewer count: Brime, Twitch.
    Follow/Subscribe Support: Brime, Twitch.

<h3>OBS Browser Links:</h3>

    Open http://localhost:8080 to view the combined chat
    Open http://localhost:8080/Notifications.html to view notification information (Follow/Subscribe/Viewer Count)

<h3>Browser Options:</h3>

    ?darkmode = use darkmode display (black background)
    ?nofade   = ignore message fading (disconnect message will still auto-fade)
         
When used in OBS, recommended to set your Browser Dock to: http://localhost:8080?nofade while your Browser Source for
display on stream would normally just be: http://localhost:8080

<h2>Console Version</h2>

Parameters to link chats:

    --brime      (Default: none) Brime login name
    --trovo      (Default: false) Add Trovo chat (uses authenticated user)
    --twitch     (Default: false) Add Twitch chat (uses authenticated user)
    --youtube    (Default: false) Activate YouTube connection
    --verbose    (Default: false) Prints all messages to standard output.
    --help       Display this help screen.
    --version    Display version information.
    
Type QUIT to close the multi-chat window (case-insensitive)
