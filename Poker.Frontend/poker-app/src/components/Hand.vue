<template>
    <div class="hand">
        <img v-for="(card, index) in cards" :key="index" :src="getCardImage(card)" :alt="card" class="card-image" />
    </div>
</template>

<script setup lang="ts">
import { defineProps } from 'vue';
defineProps({
    cards: {
        type: Array as () => string[],
        required: true
    }
});

function getCardImage(card: string): string {
    let returnString = "/cards/"
    let suitString = "_of_"
    // Map card types to their corresponding image paths

    switch (card.charAt(card.length - 1)) {
        case 'S':
            suitString += "spades.png";
            break;
        case 'H':
            suitString += "hearts.png";
            break;
        case 'D':
            suitString += "diamonds.png";
            break;
        case 'C':
            suitString += "clubs.png";
            break;
        default:
            break
    }
    switch (card.substring(0, card.length - 1)) {
        case 'Ace':
            returnString += "ace" + suitString;
            break;
        case 'King':
            returnString += "king" + suitString;
            break;
        case 'Queen':
            returnString += "queen" + suitString;
            break;
        case 'Jack':
            returnString += "jack" + suitString;
            break;
        case 'Ten':
            returnString += "10" + suitString;
            break;
        case 'Nine':
            returnString += "9" + suitString;
            break;
        case 'Eight':
            returnString += "8" + suitString;
            break;
        case 'Seven':
            returnString += "7" + suitString;
            break;
        case 'Six':
            returnString += "6" + suitString;
            break;
        case 'Five':
            returnString += "5" + suitString;
            break;
        case 'Four':
            returnString += "4" + suitString;
            break;
        case 'Three':
            returnString += "3" + suitString;
            break;
        case 'Two':
            returnString += "2" + suitString;
            break;
        default:
            returnString += card.substring(0, card.length - 1) + suitString;
    }



    return returnString;
}
</script>

<style scoped>
.hand {
    display: flex;
    gap: 10px;
}

.card-image {
    width: 100px;
    height: auto;
}
</style>