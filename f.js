const signalR = require('@microsoft/signalr')
const connection =  new signalR.HubConnectionBuilder() .withUrl("http://localhost:5160/pokerhub") .withAutomaticReconnect() .build();
console.log(signalR,connection)

connection.on("GameStateUpdated", (gameState) => {
    console.log('Game state updated:', gameState);
    // Update your UI with the new game state
});
// Start the connection
connection.start()
    .then(() => {
// connection.invoke("JoinGame", "Player1");

connection.invoke("StartGame","1");
connection.invoke("JoinGame","1")
connection.invoke("PlayerAction","1","bet",12)
})
// Start a new game

// Make a move
// connection.invoke("PlayerAction", "raise", 100);
//
//     })
//     .catch(err => console.error('Error connecting to SignalR:', err));

// Join the game

// Listen for game state updates

