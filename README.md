# MultiChat
 Merged chat system for multi-streaming support.

    Chat Support: Brime, Twitch, Trovo. May support YouTube, however this has not been tested.
    Viewer count: Brime, Twitch.
    Follow/Subscribe Support: Brime only.

Parameters to link chats:

    --brime      (Default: none) Brime login name
    --trovo      (Default: none) Trovo login name
    --twitch     (Default: none) Twitch login name
    --youtube    (Default: false) Activate YouTube connection
    --verbose    (Default: false) Prints all messages to standard output.
    --help       Display this help screen.
    --version    Display version information.

OBS Browser Links:

    Open http://localhost:8080 to view the combined chat
    Open http://localhost:8080/Notifications.html to view notification information (Follow/Subscribe/Viewer Count)

Browser Options: 

    ?darkmode = use darkmode display (black background)
    ?nofade   = ignore message fading (disconnect message will still auto-fade)
         
When used in OBS, recommended to set your Browser Dock to: http://localhost:8080?darkmode&nofade while your Browser Source for
display on stream would normally just be: http://localhost:8080

Type QUIT to close the multi-chat window (case-insensitive)
