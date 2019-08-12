$(document).ready(function() {
    "use strict";

    var canvas = document.getElementById("imgCanvas");
    var canvasWidth = canvas.width;
    var canvasHeight = canvas.height;
    var context = canvas.getContext("2d");

    var debugEnabled = false;
    var paused = false;

    var connection = new signalR.HubConnectionBuilder().withUrl("/solarHub").build();

    var playerId;

    //Disable send button until connection is established
    document.getElementById("sendButton").disabled = true;

    //going to try to make 1 pixel = 1 million kilometers approx in distance between bodies

    var displayOffsetBody;

    connection.on("Message",
        function(user, message) {
            var msg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
            var encodedMsg = user + " says " + msg;
            var li = document.createElement("li");
            li.textContent = encodedMsg;
            document.getElementById("messagesList").appendChild(li);
        });

    function trackerAndCanvasXOffset() {
        if (typeof displayOffsetBody !== "undefined") {
            return displayOffsetBody.xPosition - canvasWidth / 2;
        }
        return -canvasWidth / 2;
    }

    function trackerAndCanvasYOffset() {
        if (typeof displayOffsetBody !== "undefined") {
            return displayOffsetBody.yPosition - canvasHeight / 2;
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

            context.translate(trackerAndCanvasXOffset(), trackerAndCanvasYOffset());
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
            context.translate(body.xPosition, body.yPosition);
            context.rotate(body.angle * Math.PI / 180);
            context.translate(-body.xPosition, -body.yPosition);

            //draw ship body
            context.moveTo(body.xPosition + body.radius, body.yPosition);
            context.lineTo(body.xPosition - body.radius, body.yPosition - body.radius);
            context.lineTo(body.xPosition - body.radius, body.yPosition + body.radius);
            context.fillStyle = body.color;
            context.fill();

            //draw effects
            if (body.upPressed) {
                context.moveTo(body.xPosition - body.radius, body.yPosition);
                context.lineTo(body.xPosition - body.radius - body.radius * 3, body.yPosition);
            }
            if (body.downPressed) {
                context.moveTo(body.xPosition + body.radius, body.yPosition);
                context.lineTo(body.xPosition + body.radius + body.radius * 3, body.yPosition);
            }
            if (body.leftPressed) {
                context.moveTo(body.xPosition + body.radius, body.yPosition);
                context.lineTo(body.xPosition + body.radius, body.yPosition + body.radius * 1.5);
                context.moveTo(body.xPosition - body.radius, body.yPosition - body.radius);
                context.lineTo(body.xPosition - body.radius, body.yPosition - body.radius - body.radius * 0.5);
            }
            if (body.rightPressed) {
                context.moveTo(body.xPosition + body.radius, body.yPosition);
                context.lineTo(body.xPosition + body.radius, body.yPosition - body.radius * 1.5);
                context.moveTo(body.xPosition - body.radius, body.yPosition + body.radius);
                context.lineTo(body.xPosition - body.radius, body.yPosition + body.radius + body.radius * 0.5);
            }
            context.strokeStyle = "blue";
            context.stroke();

            //restore rotation
            context.restore();
        } else { //else if celestial body
            context.arc(body.xPosition,
                body.yPosition,
                body.radius,
                0,
                2 * Math.PI);
            context.fillStyle = body.color;
            context.fill();
        }
    }

    connection.start().then(function() {
        connection.invoke("GetConnectionId")
            .then(function(connectionId) {
                playerId = connectionId;
            });
        document.getElementById("sendButton").disabled = false;
    }).catch(function(err) {
        return console.error(err.toString());
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
        }
    }

    function forwardBoosting() { return keyMap[38] };

    function backwardBoosting() { return keyMap[40] };

    function leftTurning() { return keyMap[37] };

    function rightTurning() { return keyMap[39] };

    function shooting() { return keyMap[32] };

//todo: write a left click handler that changes the display offset body to the clicked body
});