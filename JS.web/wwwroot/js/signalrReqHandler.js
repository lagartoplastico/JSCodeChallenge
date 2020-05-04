var connection = new signalR.HubConnectionBuilder()
    .withUrl('/ChatHub')
    .build();

connection.on('receiveMessage', addMessageToChat);

connection.start()
    .catch(error => {
        console.error(error.message)
    });

const username = userName; // userName is declared in razor view.
const textInput = document.getElementById('messageText');
const chat = document.getElementById('chat');
const messagesQueue = [];
const commandFilter = '/stock=';

class Message {
    constructor(username, text, timestamp) {
        this.userName = username;
        this.text = text;
        this.timestamp = timestamp;
    }
}

function addMessageToChat(message) {
    console.log(message)
    let isCurrentUserMessage = message.username === username;
    let container = document.createElement('div');

    container.className = isCurrentUserMessage ? "container" : "container text-right";

    let d = new Date(message.timestamp);

    let timestamp = (d.getMonth() + 1) + "/"
        + d.getDate() + "/"
        + d.getFullYear() + " "
        + d.toLocaleString('en-US', { hour: 'numeric', minute: 'numeric', second: 'numeric', hour12: true })
    console.log(timestamp);

    let senderAndTime = document.createElement('p');

    senderAndTime.className = "m-0 small font-weight-bold";
    senderAndTime.innerHTML = message.username + " (" + timestamp + ")";

    let text = document.createElement('p');

    text.className = "m-0"
    text.innerHTML = message.text;

    container.appendChild(senderAndTime);
    container.appendChild(text);
    chat.appendChild(container);
    console.log(chat.childElementCount)
    if (chat.childElementCount > 5) {
        chat.removeChild(chat.children.item(1));
    }
}

function clearInputField() {
    messagesQueue.push(textInput.value);
    textInput.value = "";
}

function sendMessage() {
    let text = messagesQueue.shift() || "";

    if (text.trim() === "" || text.startsWith(commandFilter)) return;

    let timestamp = new Date();
    let message = new Message(username, text, timestamp);
    sendMessageToHub(message);
}

function sendMessageToHub(message) {
    connection.invoke('SendMessage', message);
}