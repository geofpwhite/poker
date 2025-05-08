import * as signalr from "@microsoft/signalr";
import { ref } from "vue";

let url = "192.168.1.62"
const connection = new signalr.HubConnectionBuilder()
    .withUrl("http://" + url + ":8080/pokerhub") // Replace with your backend URL
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