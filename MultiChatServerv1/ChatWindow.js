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
            case "CHAT":
                appendChatMessage(msg);
                break;
        }
    };

    ws.onclose = function (e) {
        console.log('Socket is closed. Reconnect will be attempted in 5 second.', e.reason);
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

function appendChatMessage(msg) {
    const div = document.createElement("div");
    const uname = document.createElement("span");
    const message = document.createElement("span");

    div.classList = "chat";

    uname.innerText = msg.sender.displayname + ": ";
    uname.classList = "username";
    if (msg.sender.color !== "") {
        uname.style = "color: " + msg.sender.color + ";";
    }

    for (const item of msg.emotes) {
        console.log("Name: " + item.name + " <" + item.link + ">");
        msg.message = msg.message.split(item.name).join(`<img title="${item.name}" src="${item.link}" />`);
    }

    message.classList = "message"
    message.innerHTML = msg.message + " ";

    msg.sender.badges.forEach((badge) => {
        let icon = document.createElement("img");
        icon.src = badge;
        icon.classList = "badge";
        div.appendChild(icon);
    });

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