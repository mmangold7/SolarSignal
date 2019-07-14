"use strict";

var canvas = document.getElementById("imgCanvas");
var context = canvas.getContext("2d");

var connection = new signalR.HubConnectionBuilder().withUrl("/solarHub").build();

//Disable send button until connection is established
document.getElementById("sendButton").disabled = true;


//going to try to make 1 pixel = 1 million kilometers approx in distance between bodies


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
        context.clearRect(0, 0, canvas.width, canvas.height);
        context.fillStyle = "black";
        context.fillRect(0, 0, canvas.width, canvas.height);
        bodies.forEach(body => drawBody(body));
    });

function drawBody(body) {
    context.beginPath();
    context.arc(body.xPosition + 500, body.yPosition + 375, body.radius, 0, 2 * Math.PI);
    context.fillStyle = body.color;
    context.fill();
    console.log("drew body: " + body);
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