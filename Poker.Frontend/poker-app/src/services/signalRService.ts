import * as signalR from "@microsoft/signalr";
import { ref } from "vue";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:8080/pokerhub") // Replace with your backend URL
    .withAutomaticReconnect()
    .build();

export const playerId = ref<string>("player1");
export const gameId = ref("game123");
export async function startSignalRConnection() {
    try {
        await connection.start();
        console.log("SignalR connected");
    } catch (err) {
        console.error("SignalR connection failed: ", err);
        setTimeout(startSignalRConnection, 5000); // Retry connection
    }
}

export function onEvent(eventName: string, callback: (...args: any[]) => void) {
    connection.on(eventName, callback);
}

export function sendEvent(eventName: string, ...args: any[]) {
    connection.invoke(eventName, ...args).catch((err) => console.error(err));
}