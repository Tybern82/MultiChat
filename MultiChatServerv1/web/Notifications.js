var doFade = true;

function connect() {
    var ws = new WebSocket('ws://localhost:8081');
    ws.onopen = function () {
        // connection established
        console.log('Server connected');
    };

    ws.onmessage = function (e) {
        console.log('Message from server: ', event.data);
        var msg = JSON.parse(event.data);

        switch (msg.type) {
            case "FOLLOW":
                appendFollowMessage(msg);
                break;

            case "SUBSCRIBE":
                appendSubscribeMessage(msg);
                break;

            case "VIEWERCOUNT":
                updateViewerCount(msg);
                break;

            case "ALERT":
                appendAlertMessage(msg);
                break;

            case "RAID":
                appendRaidMessage(msg);
                break;
        }
    };

    ws.onclose = function (e) {
        console.log('Socket is closed. Reconnect will be attempted in 5 second.', e.reason);
        appendDisconnectMessage();
        setTimeout(function () {
            connect();
        }, 5000);
    };

    ws.onerror = function (err) {
        console.error('Socket encountered error: ', err.message, 'Closing socket');
        ws.close();
    };

    const params = new URLSearchParams(window.location.search)
    if (params.has("darkmode")) {
        document.body.style.backgroundColor = "rgb(0,0,0)";
    }
    if (params.has("nofade")) {
        doFade = false;
    }
}

window.addEventListener("load", connect, false);

function updateViewerCount(msg) {
    const div = document.getElementById("viewerCount");
    div.innerHTML = "Combined Viewer Count: " + msg.viewerCount;
}

function appendDisconnectMessage() {
    const div = document.createElement("div");
    div.innerText = "Server has disconnected.";
    animateSlide(div);
    disappear(div, 20000);
    document.getElementById("div").appendChild(div);
}

function appendAlertMessage(msg) {
    const div = document.createElement("div");
    div.innerHTML = msg.alert;
    animateSlide(div);
    if (doFade) disappear(div, msg.mstimeout);
    document.getElementById("div").appendChild(div);
}

function appendFollowMessage(msg) {
    const div = document.createElement("div");
    const uname = document.createElement("span");
    const message = document.createElement("span");

    div.classList = "notification";

    uname.innerText = msg.username + ": ";
    uname.classList = "username";

    message.classList = "message";
    message.innerHTML = "has followed";

    div.appendChild(uname);
    div.appendChild(message);

    animateSlide(div);
    if (doFade) disappear(div, msg.mstimeout);

    document.body.style.bottom = "0px;";
    document.getElementById("div").appendChild(div);
}

function appendRaidMessage(msg) {
    const div = document.createElement("div");
    const uname = document.createElement("span");
    const message = document.createElement("span");

    div.classList = "notification";

    uname.innerText = msg.username;
    uname.classList = "username";

    message.classList = "message";
    message.innerHTML = "has raided the channel with " + msg.viewerCount + " viewers";

    div.appendChild(uname);
    div.appendChild(message);

    animateSlide(div);
    if (doFade) disappear(div, msg.mstimeout);

    document.body.style.bottom = "0px;";
    document.getElementById("div").appendChild(div);
}

function appendSubscribeMessage(msg) {
    const div = document.createElement("div");
    const uname = document.createElement("span");
    const message = document.createElement("span");

    div.classList = "notification";

    uname.innerText = msg.username + ": ";
    uname.classList = "username";

    message.classList = "message";
    message.innerHTML = "has " + (msg.isGift ? "has been gifted a sub" : (msg.isResub ? "resubscribed" : "subscribed"));
    if (msg.months > 0)
        message.innerHTML += "<br />They have been subscribed for " + msg.months + " months";

    div.appendChild(uname);
    div.appendChild(message);

    animateSlide(div);
    if (doFade) disappear(div, msg.mstimeout);

    document.body.style.bottom = "0px;";
    document.getElementById("div").appendChild(div);
}

function disappear(div, timeoutMS) {
    if (timeoutMS !== 0) {
        setTimeout(() => {
            anime({
                targets: div,
                opacity: 0,
                easing: 'easeOutExpo'
            }).finished.then(() => {
                div.remove();
            });
        }, timeoutMS);
    }
}

function animateSlide(div, direction) {
    if (direction === "Left") {
        div.style.transform = "translateX(" + window.innerWidth + "px)";
    } else {
        div.style.transform = "translateX(" + div.offsetWidth + "px)";
    }

    anime({
        targets: div,
        translateX: 0,
        easing: "easeOutExpo"
    });
}

function changeFont(fontname) {
    document.documentElement.style = "font-family: '" + fontname + "';";
    if (WebFont) {
        WebFont.load({ google: { families: [fontname] } });
    }
}