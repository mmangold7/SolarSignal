$(document).ready(function() {
    "use strict";

    //canvas stuff
    var canvas = document.getElementById("imgCanvas");
    var canvasWidth = canvas.width;
    var canvasHeight = canvas.height;
    var context = canvas.getContext("2d");

    //"global" vars
    var connection = new signalR.HubConnectionBuilder().withUrl("/solarHub").build();
    var playerId;
    var debugEnabled = false;
    var paused = false;
    var shouldDrawFuturePaths = false;
    var displayOffsetBody;
    var firstUpdate = true;

    //Disable send button until connection is established
    document.getElementById("sendButton").disabled = true;
    //going to try to make 1 pixel = 1 million kilometers approx in distance between bodies

    connection.start().then(function() {
        connection.invoke("GetConnectionId")
            .then(function(connectionId) {
                playerId = connectionId;
            });
        document.getElementById("sendButton").disabled = false;
    }).catch(function(err) {
        return console.error(err.toString());
    });

    function trackerAndCanvasXOffset() {
        if (typeof displayOffsetBody !== "undefined") {
            return displayOffsetBody.position.x - canvasWidth / 2;
        }
        return -canvasWidth / 2;
    }

    function trackerAndCanvasYOffset() {
        if (typeof displayOffsetBody !== "undefined") {
            return displayOffsetBody.position.y - canvasHeight / 2;
        }
        return -canvasHeight / 2;
    }

    connection.on("GameState",
        function(bodies) {
            //center view on player
            displayOffsetBody = bodies.filter(body => body.id === playerId)[0];

            //center view on sun
            //displayOffsetBody = bodies.filter(body => body.name === "sun")[0];

            //draw black background
            context.clearRect(0, 0, canvas.width, canvas.height);
            context.fillStyle = "black";
            context.fillRect(0, 0, canvas.width, canvas.height);

            //translate origin to the tracked body/ship
            context.translate(-trackerAndCanvasXOffset(), -trackerAndCanvasYOffset());

            //draw debug info
            //context.font = "10px Arial";
            //context.fillText("Hello World", 10, 50);
            if (debugEnabled) {
                drawGrid();
            }

            //draw bodies
            bodies.forEach(body => drawBody(body));

            //draw future paths
            if (shouldDrawFuturePaths) {
                bodies.forEach(body => drawFuturePaths(body));
            }

            //restore translation before next frame
            context.translate(trackerAndCanvasXOffset(), trackerAndCanvasYOffset());
            //console.log(bodies);

            if (firstUpdate && displayOffsetBody !== "undefined" && displayOffsetBody !== null) {
                togglePaused();
                firstUpdate = false;
            }
        });

    function drawGrid() {
        var gridSpacing = 100;
        var gridRadius = 1000;

        for (var i = -gridRadius / gridSpacing; i <= gridRadius / gridSpacing; i++) {
            context.moveTo(i * gridSpacing, -gridRadius);
            context.lineTo(i * gridSpacing, gridRadius);
            context.moveTo(-gridRadius, i * gridSpacing);
            context.lineTo(gridRadius, i * gridSpacing);
        }

        context.strokeStyle = "white";
        context.stroke();
    }

    function drawBody(body) {
        context.beginPath();

        if (body.hasOwnProperty("id")) { //if player
            //draw a triangle shaped ship
            context.save();

            //rotate drawing plane to match ship angle
            context.translate(body.position.x, body.position.y);
            context.rotate(body.angle * Math.PI / 180);
            context.translate(-body.position.x, -body.position.y);

            //draw ship body
            context.moveTo(body.position.x + body.radius, body.position.y);
            context.lineTo(body.position.x - body.radius, body.position.y - body.radius);
            context.lineTo(body.position.x - body.radius, body.position.y + body.radius);
            context.fillStyle = body.color;
            context.fill();

            //draw effects
            if (body.upPressed) {
                context.moveTo(body.position.x - body.radius, body.position.y);
                context.lineTo(body.position.x - body.radius - body.radius * 3, body.position.y);
            }
            if (body.downPressed) {
                context.moveTo(body.position.x + body.radius, body.position.y);
                context.lineTo(body.position.x + body.radius + body.radius * 3, body.position.y);
            }
            if (body.leftPressed) {
                context.moveTo(body.position.x + body.radius, body.position.y);
                context.lineTo(body.position.x + body.radius, body.position.y + body.radius * 1.5);
                context.moveTo(body.position.x - body.radius, body.position.y - body.radius);
                context.lineTo(body.position.x - body.radius, body.position.y - body.radius - body.radius * 0.5);
            }
            if (body.rightPressed) {
                context.moveTo(body.position.x + body.radius, body.position.y);
                context.lineTo(body.position.x + body.radius, body.position.y - body.radius * 1.5);
                context.moveTo(body.position.x - body.radius, body.position.y + body.radius);
                context.lineTo(body.position.x - body.radius, body.position.y + body.radius + body.radius * 0.5);
            }
            context.strokeStyle = "blue";
            context.stroke();

            //restore rotation
            context.restore();
        } else { //else if celestial body
            context.arc(body.position.x,
                body.position.y,
                body.radius,
                0,
                2 * Math.PI);
            context.fillStyle = body.color;
            context.fill();
        }
    }

    function drawFuturePaths(body) {
        var futures = body.futurePositions;
        if (futures !== "undefined" && futures !== null && futures.length !== 0) {
            context.beginPath();
            var firstPosition = futures[0];
            context.moveTo(firstPosition.x, firstPosition.y);
            for (var i = 1; i < futures.length; i++) {
                var nextPosition = futures[i];
                context.lineTo(nextPosition.x, nextPosition.y);
            }
            context.strokeStyle = body.color;
            context.stroke();
        }
    }

    window.addEventListener("keydown", handleKey, false);
    window.addEventListener("keyup", handleKey, false);

    //https://stackoverflow.com/questions/5203407/how-to-detect-if-multiple-keys-are-pressed-at-once-using-javascript
    var keyMap = {};

    function handleKey(e) {

        e.preventDefault();
        keyMap[e.keyCode] = e.type === "keydown";
        if (keyMap[37]) {
            connection.invoke("Left").catch(function(err) {
                return console.error(err.toString());
            });
        }
        if (keyMap[38]) {
            connection.invoke("Up").catch(function(err) {
                return console.error(err.toString());
            });
        }
        if (keyMap[39]) {
            connection.invoke("Right").catch(function(err) {
                return console.error(err.toString());
            });
        }
        if (keyMap[40]) {
            connection.invoke("Down").catch(function(err) {
                return console.error(err.toString());
            });
        }
        if (keyMap[68]) { //D
            debugEnabled = !debugEnabled;
        }
        if (keyMap[32]) { //Space
            connection.invoke("Shoot").catch(function(err) {
                return console.error(err.toString());
            });
        }
        if (keyMap[80]) { //P
            togglePaused();
        }
        if (keyMap[70]) { //F
            shouldDrawFuturePaths = !shouldDrawFuturePaths;
            connection.invoke("ToggleCalculateFuturePaths").catch(function(err) {
                return console.error(err.toString());
            });
        }
        if (keyMap[107] || keyMap[187]) { //+
            connection.invoke("IncreaseFuturesCalculations").catch(function(err) {
                return console.error(err.toString());
            });
        }
        if (keyMap[109] || keyMap[189]) { //-
            connection.invoke("DecreaseFuturesCalculations").catch(function(err) {
                return console.error(err.toString());
            });
        }
    }

    function togglePaused() {
        if (paused) {
            connection.invoke("Resume").catch(function(err) {
                return console.error(err.toString());
            });
        } else {
            connection.invoke("Pause").catch(function(err) {
                return console.error(err.toString());
            });
        }
        paused = !paused;
    };

    connection.on("Message",
        function(user, message) {
            var msg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
            var encodedMsg = user + " says " + msg;
            var li = document.createElement("li");
            li.textContent = encodedMsg;
            document.getElementById("messagesList").appendChild(li);
        });

    document.getElementById("sendButton").addEventListener("click",
        function(event) {
            var user = document.getElementById("userInput").value;
            var message = document.getElementById("messageInput").value;
            connection.invoke("Message", user, message).catch(function(err) {
                return console.error(err.toString());
            });
            event.preventDefault();
        });

//todo: write a left click handler that changes the display offset body to the clicked body
//todo:add floating indicators for player names and integrate chat with the game
//todo:allow customizing ship colors and maybe appearance
//if i've already calculated the next however many paths, why bother continuing to do so when i could just cache the future positions, perhaps by using an array indexed with the iteration of the main loop.
//not only could i stop calculated futures, I could stop calculating the actual paths! just used the already calculated values
//need to add unit tests so things don't break like the pause function or the grid etc
//cache paths on the client rather than server
//bug: it starts behaving the way i want with future paths after i hit increment or decrement via plus or minus. need that on all the time
//think about how slower things get shorter paths. maybe path length should be constants not positions
});