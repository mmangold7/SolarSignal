$(document).ready(function() {
    "use strict";

    //canvas stuff
    var canvas = document.getElementById("imgCanvas");
    var canvasWidth = canvas.width;
    var canvasHeight = canvas.height;
    var context = canvas.getContext("2d");

    var gridSpacing = 100;
    var gridRadius = 10000;

    var starsList = [];

    Math.seedrandom('any string you like');
    for (var i = 0; i < 10000; i++) {
        starsList.push([Math.random() * gridRadius, Math.random() * gridRadius]);
        starsList.push([-Math.random() * gridRadius, Math.random() * gridRadius]);
        starsList.push([Math.random() * gridRadius, -Math.random() * gridRadius]);
        starsList.push([-Math.random() * gridRadius, -Math.random() * gridRadius]);
    }

    //other "global" vars
    var connection = new signalR.HubConnectionBuilder().withUrl("/solarHub").build();
    var playerId;
    var debugEnabled = false;
    var shouldDrawFuturePaths = false;
    var displayOffsetBody;
    var firstUpdate = true;
    var cachedBodyFutures = {};
    var isBuildingShip = true;

    //Disable send button until connection is established
    //document.getElementById("sendButton").disabled = true;
    //going to try to make 1 pixel = 1 million kilometers approx in distance between bodies

    connection.start().then(function() {
        connection.invoke("GetConnectionId")
            .then(function(connectionId) {
                playerId = connectionId;
            });
        //document.getElementById("sendButton").disabled = false;
    }).catch(function(err) {
        return console.error(err.toString());
    });

    $("#playButton").click(function() {
        var colorPickerColor = $("#shipColorPicker").css("background-color");
        connection.invoke("CreatePlayerWithId", playerId, colorPickerColor).then(function() {
            isBuildingShip = false;
        }).catch(function(err) {
            return console.error(err.toString());
        });
    });

    context.clearRect(0, 0, canvas.width, canvas.height);
    context.fillStyle = "black";
    context.fillRect(0, 0, canvas.width, canvas.height);
    context.fill();

    context.fillStyle = "black";
    context.font = "36px Arial";
    context.fillRect(0, 0, canvas.width, canvas.height);
    context.fillStyle = "white";
    context.fillText("PAUSED AT START", canvasWidth / 2 - 250, canvasHeight/2 - 250);
    context.fillText("PRESS P TO UN-PAUSE", canvasWidth / 2 - 100, canvasHeight / 2 + 40);
    context.fill();

    var keyMap = {};
    connection.on("GameState",
        function (bodies, alreadyCalculatedPaths) {
            context.canvas.width = window.innerWidth;
            context.canvas.height = window.innerHeight;
            canvasWidth = window.innerWidth;
            canvasHeight = window.innerHeight;

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

            drawStars();

            //draw future paths
            if (shouldDrawFuturePaths) {
                if (alreadyCalculatedPaths && cachedBodyFutures[0] !== 0) {
                    Object.keys(cachedBodyFutures).forEach(function (bodyName) {
                        drawFuturePaths(cachedBodyFutures[bodyName], bodies.filter(b => b.name == bodyName)[0].color);
                    });
                } else {
                    bodies.forEach(body => drawFuturePaths(body.futurePositions, body.color));
                }
            }

            //draw bodies
            bodies.forEach(body => drawBody(body));

            //restore translation before next frame
            context.translate(trackerAndCanvasXOffset(), trackerAndCanvasYOffset());

            if (firstUpdate && displayOffsetBody !== "undefined" && displayOffsetBody !== null) {
                //Put things here you only want to happen once
                firstUpdate = false;
            }

            bodies.forEach(function(body) {
                if (body.futurePositions !== undefined && body.futurePositions !== null) {
                    cachedBodyFutures[body.name] = body.futurePositions;
                }
            });

            if (!isBuildingShip) {
                var inputKeyMap = {
                    "LeftPressed": keyMap[37],
                    "UpPressed": keyMap[38],
                    "RightPressed": keyMap[39],
                    "DownPressed": keyMap[40]
                };

                connection.invoke("Input", inputKeyMap).catch(function(err) {
                    return console.error(err.toString());
                });
            }
            if (alreadyCalculatedPaths && cachedBodyFutures[0] !== 0) {
                Object.keys(cachedBodyFutures).forEach(function(bodyName) {
                    cachedBodyFutures[bodyName].shift();
                });
            };
        });

    function drawGrid() {
        context.beginPath();
        for (var i = -gridRadius / gridSpacing; i <= gridRadius / gridSpacing; i++) {
            context.moveTo(i * gridSpacing, -gridRadius);
            context.lineTo(i * gridSpacing, gridRadius);
            context.moveTo(-gridRadius, i * gridSpacing);
            context.lineTo(gridRadius, i * gridSpacing);
        }

        context.strokeStyle = "white";
        context.stroke();
    }

    function drawStars() {
        context.fillStyle = "white";
        starsList.forEach(function(star) {
            context.fillRect(star[0], star[1], 2, 2);
        });
        context.fill();
    }

    function drawBody(body) {
        context.beginPath();
        if (body.hasOwnProperty("id")) { //if player
            //draw a triangle shaped ship
            context.save();
            context.beginPath();
            context.strokeStyle = body.color;

            //rotate drawing plane to match ship angle
            context.translate(body.position.x, body.position.y);
            context.rotate(body.angle * Math.PI / 180);
            context.translate(-body.position.x, -body.position.y);

            //draw effects
            context.beginPath();
            if (body.input.upPressed) {
                context.moveTo(body.position.x - body.radius, body.position.y);
                context.lineTo(body.position.x - body.radius - body.radius * 3, body.position.y);
            }
            if (body.input.downPressed) {
                context.moveTo(body.position.x + body.radius, body.position.y);
                context.lineTo(body.position.x + body.radius + body.radius * 3, body.position.y);
            }
            if (body.input.leftPressed) {
                context.moveTo(body.position.x + body.radius, body.position.y);
                context.lineTo(body.position.x + body.radius, body.position.y + body.radius * 1.75);
                context.moveTo(body.position.x - body.radius, body.position.y - body.radius);
                context.lineTo(body.position.x - body.radius, body.position.y - body.radius - body.radius * 0.75);
            }
            if (body.input.rightPressed) {
                context.moveTo(body.position.x + body.radius, body.position.y);
                context.lineTo(body.position.x + body.radius, body.position.y - body.radius * 1.75);
                context.moveTo(body.position.x - body.radius, body.position.y + body.radius);
                context.lineTo(body.position.x - body.radius, body.position.y + body.radius + body.radius * 0.75);
            }
            context.strokeStyle = "yellow";
            context.stroke();

            //draw shield outline
            context.beginPath();
            context.arc(body.position.x,
                body.position.y,
                body.radius * 2,
                0,
                2 * Math.PI);
            context.strokeStyle = "purple";
            context.stroke();

            //draw ship body
            context.beginPath();
            context.moveTo(body.position.x + body.radius * 1.2, body.position.y);
            context.lineTo(body.position.x - body.radius * 0.8, body.position.y - body.radius);
            context.lineTo(body.position.x - body.radius * 0.8, body.position.y + body.radius);
            context.fillStyle = body.color;
            context.fill();

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

    function drawFuturePaths(futures, color) {
        if (futures !== "undefined" && futures !== null && futures.length !== 0) {
            context.beginPath();
            var firstPosition = futures[0];
            context.moveTo(firstPosition.x, firstPosition.y);
            for (var i = 1; i < futures.length; i++) {
                var nextPosition = futures[i];
                context.lineTo(nextPosition.x, nextPosition.y);
            }
            context.strokeStyle = color;
            context.stroke();
        }
    }

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

    window.addEventListener("keydown", handleKey, false);
    window.addEventListener("keyup", handleKey, false);

    //https://stackoverflow.com/questions/5203407/how-to-detect-if-multiple-keys-are-pressed-at-once-using-javascript

    function handleKey(e) {
        keyMap[e.keyCode] = e.type === "keydown";
        if (keyMap[37]) {
            e.preventDefault();
        }
        if (keyMap[38]) {
            e.preventDefault();
        }
        if (keyMap[39]) {
            e.preventDefault();
        }
        if (keyMap[40]) {
            e.preventDefault();
        }
        if (keyMap[68]) { //D
            e.preventDefault();
            debugEnabled = !debugEnabled;
        }
        if (keyMap[32]) { //Space
            e.preventDefault();
            connection.invoke("Shoot").catch(function(err) {
                return console.error(err.toString());
            });
        }
        if (keyMap[80]) { //P
            e.preventDefault();
            connection.invoke("TogglePaused").catch(function(err) {
                return console.error(err.toString());
            });
        }
        if (keyMap[70]) { //F
            e.preventDefault();
            connection.invoke("ToggleCalculateFuturePaths", shouldDrawFuturePaths).catch(function(err) {
                return console.error(err.toString());
            });
        }
        if (keyMap[107] || keyMap[187]) { //+
            e.preventDefault();
            connection.invoke("IncreaseFuturesCalculations").catch(function(err) {
                return console.error(err.toString());
            });
        }
        if (keyMap[109] || keyMap[189]) { //-
            e.preventDefault();
            connection.invoke("DecreaseFuturesCalculations").catch(function(err) {
                return console.error(err.toString());
            });
        }
    }

    connection.on("ToggleCalculateFuturePaths",
        function(currentShouldCalculateFuturePaths) {
            shouldDrawFuturePaths = currentShouldCalculateFuturePaths;
        });

    //connection.on("Message",
    //    function(user, message) {
    //        var msg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    //        var encodedMsg = user + " says " + msg;
    //        var li = document.createElement("li");
    //        li.textContent = encodedMsg;
    //        document.getElementById("messagesList").appendChild(li);
    //    });

    //document.getElementById("sendButton").addEventListener("click",
    //    function(event) {
    //        var user = document.getElementById("userInput").value;
    //        var message = document.getElementById("messageInput").value;
    //        connection.invoke("Message", user, message).catch(function(err) {
    //            return console.error(err.toString());
    //        });
    //        event.preventDefault();
    //    });
});

//todo:s
//The most important thing: elastic collisions, probably with damping. give players "shields". let things falls safely together and bounce around. should be awesome

//add floating indicators for player names and integrate chat with the game
//allow customizing ship appearance

//if i've already calculated the next however many paths, why bother continuing to do so when i could just cache the future positions, perhaps by using an array indexed with the iteration of the main loop.
//not only could i stop calculated futures, I could stop calculating the actual paths! just used the already calculated values
//need to add unit tests so things don't break like the pause function or the grid etc
//think about how slower things get shorter paths. maybe path length should be constants not positions
//idea:velocity dependent line colors. make the points colored based on the velocity magnitude of the body at the simulated point. wow that would be easy! lol
//write a left click handler that changes the display offset body to the clicked body