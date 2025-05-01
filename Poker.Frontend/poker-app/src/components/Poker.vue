<script setup lang="ts">
import { ref, onMounted } from "vue";
import { gameId, startSignalRConnection, onEvent, sendEvent, playerId } from '../services/signalRService';
import Hand from './Hand.vue';
import Table from './Table.vue';
import type { GameState } from '../models/GameState';
// import '@/assets/style.css';
const messages = ref<string[]>([]);
const sliderValue = ref(50);
const myCards = ref<string[]>([]);
const communityCards = ref<string[]>([]);
const game = ref<GameState>({} as GameState);


onMounted(async () => {
  // await startSignalRConnection();

  onEvent("GameStateUpdated", (gameState) => {
    messages.value.push(`Game state updated: ${JSON.stringify(gameState)}`);
    game.value = gameState as GameState;
    communityCards.value = gameState.CommunityCards;
  });
  onEvent("GameStarted", (gameData) => {
    messages.value.push(`Game started: ${JSON.stringify(gameData)}`);
  });

  onEvent("UserJoined", (userId) => {
    messages.value.push(`User joined: ${userId}`);
  });

  onEvent("PlayerCards", (cards) => {
    myCards.value = cards;
    messages.value.push(`Your cards: ${JSON.stringify(cards)}`);
  });
  gameId.value = localStorage.getItem("gameId")!;
  console.log(playerId.value)
});

const startGame = () => {
  sendEvent("StartGame", gameId.value);
};

const bet = (amt: number) => {
  sendEvent("PlayerAction", gameId.value, "bet", amt);
};
const check = () => {
  sendEvent("PlayerAction", gameId.value, "check", 0);
};
const fold = () => {
  sendEvent("PlayerAction", gameId.value, "fold", 0);
};

const call = () => {
  sendEvent("PlayerAction", gameId.value, "call", 0);
};
const raise = (amt: number) => {
  sendEvent("PlayerAction", gameId.value, "raise", amt as number);
};

</script>

<template>
  <div class=" min-w-screen">
    <div style="color: black">
      <h1 class="text-2xl font-bold mb-4">Poker Game</h1>
      <p class="text-lg font-semibold text-white mb-4">Game ID: {{ gameId }}</p>
      <p class="text-lg font-semibold text-white mb-4">Player ID: {{ playerId }}</p>
      <p class="text-lg font-semibold text-white mb-4">Current Bet: {{ game.CurrentBet }}</p>
      <p class="text-lg font-semibold text-white mb-4">Your Chips: {{game.Players.find(p => p.Name ===
        playerId)?.Chips}}</p>
      <p class="text-lg font-semibold text-white mb-4">Current Turn: {{ game.Players[game.Turn].Id }}</p>
      <p class="text-lg font-semibold text-white mb-4">Current Turn Index: {{ game.Turn }}</p>
      <p class="text-lg font-semibold text-white mb-4">Pot: {{ game.Pot }}</p>
    </div>

    <div style="color: black">
      <p class="text-lg font-semibold text-white mb-4">Value: {{ sliderValue }}</p>
      <input type="range" v-model="sliderValue" min="0" max="100" step="1">
    </div>

    <div class="flex flex-wrap gap-2 mb-4">
      <button type="button" @click="startGame" class="px-4 py-2 bg-blue-500 text-white rounded hoveri:bg-blue-600">Start
        Game
      </button>
      <button type="button" @click="check"
        class="px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600">Check</button>
      <button type="button" @click="fold" class="px-4 py-2 bg-red-500 text-white rounded hover:bg-red-600">Fold</button>
      <button type="button" @click="call"
        class="px-4 py-2 bg-yellow-500 text-white rounded hover:bg-yellow-600">Call</button>
      <button type="button" @click="raise(sliderValue as number)"
        class="px-4 py-2 bg-purple-500 text-white rounded hover:bg-purple-600">Raise</button>

    </div>
    <Table :Players="game.Players" style="margin-bottom: 25px"></Table>
    <Hand :cards="communityCards" />
    <Hand :cards="myCards" class="mt-4" />
    <ul
      class="list-disc pl-5 mb-4 fixed bottom-0 right-0 bg-white shadow-lg rounded-lg p-4 max-h-64 overflow-y-auto max-w-[25vw] whitespace-normal">
      <li v-for="(message, index) in messages" :key="index" class="text-gray-700 whitespace-normal">{{ message }}</li>
    </ul>
  </div>
</template>
