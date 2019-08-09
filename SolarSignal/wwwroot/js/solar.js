"use strict";

var canvas = document.getElementById("imgCanvas");
var canvasWidth = canvas.width;
var canvasHeight = canvas.height;
var context = canvas.getContext("2d");

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
    function (bodies) {
        //center view on player
        if (displayOffsetBody === null || displayOffsetBody === undefined) {
            displayOffsetBody = bodies.filter(body => body.id === "sun")[0];
        }

        //draw black background
        context.clearRect(0, 0, canvas.width, canvas.height);
        context.fillStyle = "black";
        context.fillRect(0, 0, canvas.width, canvas.height);

        //draw bodies
        bodies.forEach(body => drawBody(body));

        //draw debug info
        //context.font = "10px Arial";
        //context.fillText("Hello World", 10, 50);
    });

function drawBody(body) {
    context.beginPath();

    var xOffset = displayOffsetBody.xPosition - canvasWidth/2;
    var yOffset = displayOffsetBody.yPosition - canvasHeight/2;

    //if player
    if (body.hasOwnProperty('id')) {
        //
        context.save();
        context.translate(body.xPosition, body.yPosition);
        context.rotate(body.angle * Math.PI / 180);

        //draw a triangle shaped ship
        context.moveTo(body.xPosition + body.radius - xOffset, body.yPosition - yOffset);
        context.lineTo(body.xPosition - body.radius - xOffset, body.yPosition - body.radius - yOffset);
        context.lineTo(body.xPosition - body.radius - xOffset, body.yPosition + body.radius - yOffset);
        context.fillStyle = body.color;
        context.fill();

        //
        context.translate(-body.xPosition, -body.yPosition);
        context.restore();
    //else if celestial body
    } else {
        context.arc(body.xPosition - xOffset, body.yPosition - yOffset, body.radius, 0, 2 * Math.PI);
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