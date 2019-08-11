﻿$(document).ready(function() {
    "use strict";

    var canvas = document.getElementById("imgCanvas");
    var canvasWidth = canvas.width;
    var canvasHeight = canvas.height;
    var context = canvas.getContext("2d");

    var debugEnabled = false;

    var connection = new signalR.HubConnectionBuilder().withUrl("/solarHub").build();

    var playerId;

    //Disable send button until connection is established
    document.getElementById("sendButton").disabled = true;

    //going to try to make 1 pixel = 1 million kilometers approx in distance between bodies

    var displayOffsetBody;

    //todo:refactor this so it doesn't "fail silently" when the display offset body is undefined
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

    function trackerXOffset() {
        if (typeof displayOffsetBody !== "undefined") {
            return displayOffsetBody.xPosition;
        }
        return 0;
    }

    function trackerYOffset() {
        if (typeof displayOffsetBody !== "undefined") {
            return displayOffsetBody.yPosition;
        }
        return 0;
    }

    connection.on("Message",
        function(user, message) {
            var msg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
            var encodedMsg = user + " says " + msg;
            var li = document.createElement("li");
            li.textContent = encodedMsg;
            document.getElementById("messagesList").appendChild(li);
        });

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
            //context.translate(trackerAndCanvasXOffset, trackerAndCanvasYOffset);

            //draw debug info
            //context.font = "10px Arial";
            //context.fillText("Hello World", 10, 50);
            if (debugEnabled) {
                drawGrid();
            }

            //draw bodies
            bodies.forEach(body => drawBody(body));
        });

    function drawGrid() {
        var midX = canvasWidth / 2;
        var midY = canvasHeight / 2;

        context.moveTo(midX - trackerXOffset(), 0 - trackerYOffset());
        context.lineTo(midX - trackerXOffset(), canvasHeight - trackerYOffset());

        context.moveTo(0 - trackerXOffset(), midY - trackerYOffset());
        context.lineTo(canvasWidth - trackerXOffset(), midY - trackerYOffset());

        context.strokeStyle = "white";
        context.stroke();
    }

    function drawBody(body) {
        context.beginPath();

        //if player
        if (body.hasOwnProperty("id")) {
            //draw a triangle shaped ship
            context.save();

            //rotate drawing plane to match ship angle
            context.translate(body.xPosition - trackerAndCanvasXOffset(), body.yPosition - trackerAndCanvasYOffset());
            context.rotate(body.angle * Math.PI / 180);
            context.translate(-body.xPosition + trackerAndCanvasXOffset(), -body.yPosition + trackerAndCanvasYOffset());

            //draw ship body
            context.moveTo(body.xPosition + body.radius - trackerAndCanvasXOffset(),
                body.yPosition - trackerAndCanvasYOffset());
            context.lineTo(body.xPosition - body.radius - trackerAndCanvasXOffset(),
                body.yPosition - body.radius - trackerAndCanvasYOffset());
            context.lineTo(body.xPosition - body.radius - trackerAndCanvasXOffset(),
                body.yPosition + body.radius - trackerAndCanvasYOffset());
            context.fillStyle = body.color;
            context.fill();

            //draw effects
            //todo:replace longer expressions with variables
            //context.

            context.restore();
            //else if celestial body
        } else {
            context.arc(body.xPosition - trackerAndCanvasXOffset(),
                body.yPosition - trackerAndCanvasYOffset(),
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

    function forwardBoosting() { return keyMap[38] };

    function backwardBoosting() { return keyMap[40] };

    function leftTurning() { return keyMap[37] };

    function rightTurning() { return keyMap[39] };

    //https://stackoverflow.com/questions/5203407/how-to-detect-if-multiple-keys-are-pressed-at-once-using-javascript
    var keyMap = {};

    function handleKey(e) {
        e.preventDefault();
        keyMap[e.keyCode] = e.type == "keydown";
        if (keyMap[37]) {
            connection.invoke("Left", playerId).catch(function(err) {
                return console.error(err.toString());
            });
        }
        if (keyMap[38]) {
            connection.invoke("Up", playerId).catch(function(err) {
                return console.error(err.toString());
            });
        }
        if (keyMap[39]) {
            connection.invoke("Right", playerId).catch(function(err) {
                return console.error(err.toString());
            });
        }
        if (keyMap[40]) {
            connection.invoke("Down", playerId).catch(function(err) {
                return console.error(err.toString());
            });
        }
        if (keyMap[68]) {
            debugEnabled = !debugEnabled;
        }
    }

//todo: write a left click handler that changes the display offset vector to the clicked body. the vector is just the bodies position
//on click set the global variable for the body whose position vector should be used
});