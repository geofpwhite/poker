<script setup lang="ts">
import { defineProps } from 'vue';
import type { Player } from '@/models/GameState';
import { h } from 'vue';
defineProps({
    Players: {
        type: Array as () => Player[],
        required: true
    }
});

</script>
<style scoped>
/* Basic reset & centering */
body {
    margin: 0;
    display: flex;
    justify-content: center;
    /* align-items: center; */
    /* min-height: 101vh; */
    background: transparent;
}

/* The poker table rectangle */
.table {
    margin-top: 0%;
    position: relative;
    width: 400px;
    height: 200px;
    background-color: #006400;
    /* deep green */
    /* border: 2px solid #004d00; */

    /* a slightly darker border */
    border-radius: 15px;
    /* soft rounded corners */
}

/* General circle style */
.circle {
    position: absolute;
    width: 200px;
    height: 200px;
    background-color: #006400;
    /* match table color */
    /* border: 2px solid #004d00; */
    border-radius: 50%;
}

/* Position the left circle half-overlapping the left edge */
.circle.left {
    left: -98px;
    /* move left by half the circle's width */
    top: 50%;
    transform: translateY(-50%);
    background-color: #006400;
}

/* Position the right circle half-overlapping the right edge */
.circle.right {
    right: -98px;
    top: 50%;
    transform: translateY(-50%);
    background-color: #006400;
}

/* Hollow black circles */
.hollow-circle {
    position: absolute;
    width: 50px;
    height: 50px;
    border: 3px solid white;

    /* black border */
    background-color: transparent;
    /* hollow center */
    border-radius: 50%;
}

/* Position the hollow circles */
.hollow-circle.top-left {
    top: -25px;
    /* half the circle's height */
    left: -25px;
    /* half the circle's width */
}

.hollow-circle.top {
    top: -25px;
    /* half the circle's height */
    left: 45%;
}

.hollow-circle.bottom {
    bottom: -25px;
    /* half the circle's height */
    left: 45%;
}

.hollow-circle.top-right {
    top: -25px;
    right: -25px;
}

.hollow-circle.bottom-left {
    background-color: blue;
    bottom: -25px;
    left: -25px;
}

.hollow-circle.bottom-right {
    bottom: -25px;
    right: -25px;
}
</style>

<template>

    <body>
        <div class="table">
            <div class="circle left"></div>
            <div class="circle right"></div>
            <div class="hollow-circle bottom-left" v-if="Players.length >= 1"></div>
            <div class="hollow-circle top-left" v-if="Players.length >= 2"></div>
            <div class="hollow-circle top" v-if="Players.length >= 3"></div>
            <div class="hollow-circle top-right" v-if="Players.length >= 4"></div>
            <div class="hollow-circle bottom-right" v-if="Players.length >= 5"></div>
            <div class="hollow-circle bottom" v-if="Players.length >= 6"></div>
        </div>
    </body>

</template>