<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import Poker from './components/Poker.vue'
import Lobbies from './components/Lobbies.vue';
import { onMounted } from 'vue';
import { startSignalRConnection } from './services/signalRService';
import '@/assets/style.css';


onMounted(async () => {
  // This is where you can initialize any global state or perform actions when the app is mounted
  await startSignalRConnection();
});
</script>

<template>
  <header>

    <div class="wrapper">
      <!-- <Poker msg="You did it!" /> -->

      <nav>
        <!-- <RouterLink to="/">Home</RouterLink>
        <RouterLink to="/about">About</RouterLink> -->
        <!-- <RouterLink to="/lobbies">Lobbies</RouterLink> -->
      </nav>
    </div>
  </header>

  <RouterView />
</template>