# MultiChat
 Merged chat system for multi-streaming support. Provides both a GUI and console interface. Run MultiChat.exe for the GUI, or MultiChatConsole.exe for the console version.

    Chat Support: Brime, Twitch, Trovo. May support YouTube, however this has not been tested.
    Viewer count: Brime, Twitch.
    Follow/Subscribe Support: Brime, Twitch; Trovo (subscribe only).

<h3>OBS Browser Links:</h3>

    Open http://localhost:8080 to view the combined chat
    Open http://localhost:8080/Notifications.html to view notification information (Follow/Subscribe/Viewer Count)

<h3>Browser Options:</h3>

    ?darkmode = use darkmode display (black background)
    ?nofade   = ignore message fading (disconnect message will still auto-fade)
    ?modView  = controls how deleted messages are handled (if present, will mark but not remove; if absent, will just remove the message)
         
ModView option is only valid for Chat page. When used in OBS, recommended to set your Browser Dock to: http://localhost:8080?nofade while your Browser Source for
display on stream would normally just be: http://localhost:8080

Until option is added to the GUI, add bot account names to "ignoreNames" in %AppData%\Roaming\MultiChat.json to have 
them automatically removed from the viewer count supplied by Twitch. Default list includes common chat bots 
(StreamElements, StreamLabs, PretzelRocks, Nightbot, etc).

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

<h2>Building</h2>

To build this project, you will also need a copy of the BrimeAPI (https://github.com/Tybern82/BrimeAPI) library to link the MultiChatServer against. Solution includes a reference to the correct project under "..\BrimeAPI\BrimeAPI-v1\BrimeAPI-v1.csproj"
