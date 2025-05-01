<script setup lang="ts">
import { ref, onMounted } from "vue";
import * as signalR from "../services/signalRService";
import { useRouter } from "vue-router";
const games = ref<any[]>([]);
const router = useRouter();
const newLobbyName = ref("");
const playerName = ref("");
onMounted(async () => {
    try {
        const response = await fetch("/games");
        if (!response.ok) {
            throw new Error("Failed to fetch games");
        }
        games.value = await response.json();
    } catch (error) {
        console.error("Error fetching games:", error);
    }
});

signalR.onEvent("GameCreated", (gameId: string) => {
    games.value.push({ id: gameId, status: "Waiting for players" });
});
const joinGame = (game: string) => {
    signalR.sendEvent("JoinGame", signalR.playerId.value, game);
    signalR.gameId.value = game;
    localStorage.setItem("gameId", game);
    router.push("/game/" + game);
};

const newGame = () => {
    signalR.sendEvent("CreateGame", newLobbyName.value)
};
const updatePlayerName = () => {
    signalR.playerId.value = playerName.value;
};
</script>

<template>
    <div>
        <h1>Available Games</h1>
        <div>
            <label for="playerName">Player Name:</label>
            <input id="playerName" v-model="playerName" type="text" placeholder="Enter your name" />
            <button @click="updatePlayerName">Update Name</button>
        </div>
        <div>
            <label for="newLobbyName">Lobby Name:</label>
            <input id="newLobbyName" v-model="newLobbyName" type="text" placeholder="Enter new lobby name" />
            <button @click="newGame">Create Game</button>
        </div>
        <ul>
            <li v-for="(game, index) in games" :key="index">
                <button style="border: none; background: none; padding: 0; cursor: pointer;" @click="joinGame(game.id)">
                    <a style="color:white">
                        {{ game.id }} - {{ game.status }}
                    </a>
                </button>
            </li>

        </ul>
    </div>
</template>

<style scoped>
/* Add any styles for the Lobbies component here */
</style>