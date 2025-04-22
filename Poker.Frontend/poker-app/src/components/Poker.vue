<script setup lang="ts">
import { ref, onMounted } from "vue";
import { startSignalRConnection, onEvent, sendEvent } from "../services/signalRService";

const messages = ref<string[]>([]);
const sliderValue = ref(50);

onMounted(async () => {
  await startSignalRConnection();

  onEvent("GameStarted", (gameData) => {
    messages.value.push(`Game started: ${JSON.stringify(gameData)}`);
  });

  onEvent("UserJoined", (userId) => {
    messages.value.push(`User joined: ${userId}`);
  });


});

const startGame = () => {
  sendEvent("StartGame", "game123");
};
const bet = (amt: number) => { };
const check = () => { };
const fold = () => { };
const call = () => { };
const raise = (amt: number) => { };

</script>

<template>
  <div>
    <h1>SignalR Test</h1>
    <button @click="startGame">Start Game</button>
    <p>Value: {{ sliderValue }}</p>
    <input type="range" v-model="sliderValue" min="0" max="100" step="1">
    <ul>
      <li v-for="(message, index) in messages" :key="index">{{ message }}</li>
    </ul>
  </div>
</template>

<style src="../assets/Poker.css" scoped></style>
