"use strict";

var canvas = document.getElementById("imgCanvas");
var canvasWidth = canvas.width;
var canvasHeight = canvas.height;
var context = canvas.getContext("2d");

var debugEnabled = false;

var connection = new signalR.HubConnectionBuilder().withUrl("/solarHub").build();

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
    //var numberOfDivisions = 3;
    //var squareSide

    //for (var lineIterator = 0; lineIterator < numberOfDivisions; lineIterator++) {
    //    canvasWidth/numberOfDivisions
    //}
    var midX = canvasWidth / 2;
    var midY = canvasHeight / 2;

    context.moveTo(midX - trackerXOffset(), 0 - trackerYOffset());
    context.lineTo(midX - trackerXOffset(), canvasHeight - trackerYOffset());

    context.moveTo(0 - trackerXOffset(), midY - trackerYOffset());
    context.lineTo(canvasWidth - trackerXOffset(), midY - trackerYOffset());

    context.strokeStyle = "white";
    context.stroke();

}

function trackerAndCanvasXOffset() {
    return displayOffsetBody.xPosition - canvasWidth / 2;
}

function trackerAndCanvasYOffset() {
    return displayOffsetBody.yPosition - canvasHeight / 2;
}

function trackerXOffset() {
    return displayOffsetBody.xPosition;
}

function trackerYOffset() {
    return displayOffsetBody.yPosition;
}

function drawBody(body) {
    context.beginPath();

    //if player
    if (body.hasOwnProperty("id")) {
        //
        //context.save();
        //context.translate(-canvasWidth / 2, -canvasHeight / 2);
        //context.rotate(body.angle * Math.PI / 180);

        //draw a triangle shaped ship
        //context.translate(body.xPosition - trackerAndCanvasXOffset(), body.yPosition - trackerAndCanvasYOffset());
        //context.rotate(body.angle * Math.PI / 180);

        context.moveTo(body.xPosition + body.radius - trackerAndCanvasXOffset(), body.yPosition - trackerAndCanvasYOffset());
        context.lineTo(body.xPosition - body.radius - trackerAndCanvasXOffset(), body.yPosition - body.radius - trackerAndCanvasYOffset());
        context.lineTo(body.xPosition - body.radius - trackerAndCanvasXOffset(), body.yPosition + body.radius - trackerAndCanvasYOffset());
        context.fillStyle = body.color;
        context.fill();

        //
        //context.translate(canvasWidth / 2, canvasHeight / 2);
        //context.restore();
        //else if celestial body
    } else {
        context.arc(body.xPosition - trackerAndCanvasXOffset(), body.yPosition - trackerAndCanvasYOffset(), body.radius, 0, 2 * Math.PI);
        context.fillStyle = body.color;
        context.fill();
    }
}

connection.start().then(function() {
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

function handleKey(e) {

    if ([32, 37, 38, 39, 40].indexOf(e.keyCode) > -1) {
        e.preventDefault();
    }

    var code = e.keyCode;

    switch (code) {
    case 37:
        connection.invoke("Left", playerId).catch(function(err) {
            return console.error(err.toString());
        });
        break; //Left key
    case 38:
        connection.invoke("Up", playerId).catch(function(err) {
            return console.error(err.toString());
        });
        break; //Up key
    case 39:
        connection.invoke("Right", playerId).catch(function(err) {
            return console.error(err.toString());
        });
        break; //Right key
    case 40:
        connection.invoke("Down", playerId).catch(function(err) {
            return console.error(err.toString());
        });
        break; //Down key
    case 68:
        debugEnabled = !debugEnabled;
        break; //D key
    default:
        alert(code); //Everything else
    }
}

//todo: write a left click handler that changes the display offset vector to the clicked body. the vector is just the bodies position
//on click set the global variable for the body whose position vector should be used

//window.addEventListener("keydown", function (e) {
//    // space and arrow keys
//    if ([32, 37, 38, 39, 40].indexOf(e.keyCode) > -1) {
//        e.preventDefault();
//    }
//}, false);

//todo: replace some of the offset logic with context.translate to simplify the code