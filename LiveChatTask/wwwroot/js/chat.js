// Responsible for connecting to the ChatHub and calling the ChatController API.
// Public API:
//   ChatClient.initConnection(chatSessionId, onMessageReceived)
//   ChatClient.sendMessage(text)

var ChatClient = (function () {
    let connection = null;
    let connectionPromise = null;
    let currentSessionId = null;
    let receiveHandler = null;
    let presenceHandler = null;
    let messageStatusHandler = null;
    let sessionEndedHandler = null;
    let unreadCountChangedHandler = null;
    const joinedSessions = new Set();

    async function ensureConnection() {
        if (connection && connection.state === "Connected") {
            return;
        }

        if (!window.signalR) {
            console.error("SignalR client library not loaded.");
            throw new Error("SignalR client not loaded");
        }

        if (!connection) {
            connection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/chat")
                .withAutomaticReconnect()
                .build();

            connection.on("ReceiveMessage", function (chatSessionId, messageId, fromUserId, messageText, messageType, role, sentAt, status) {
                if (typeof receiveHandler === "function") {
                    receiveHandler({
                        chatSessionId,
                        messageId,
                        fromUserId,
                        messageText,
                        messageType,
                        role,
                        sentAt,
                        status
                    });
                }
            });

            connection.on("UserPresenceChanged", function (userId, status, lastSeen) {
                if (typeof presenceHandler === "function") {
                    presenceHandler({
                        userId,
                        status,
                        lastSeen
                    });
                }
            });

            connection.on("MessageStatusChanged", function (chatSessionId, messageIds, status) {
                if (typeof messageStatusHandler === "function") {
                    messageStatusHandler({
                        chatSessionId,
                        messageIds,
                        status
                    });
                }
            });

            connection.on("SessionEnded", function (chatSessionId) {
                if (typeof sessionEndedHandler === "function") {
                    sessionEndedHandler({
                        chatSessionId
                    });
                }
            });

            connection.on("UnreadCountChanged", function (userId, count) {
                if (typeof unreadCountChangedHandler === "function") {
                    unreadCountChangedHandler({
                        userId,
                        count
                    });
                }
            });

            connection.onclose(function (error) {
                console.warn("Chat connection closed.", error || "");
            });
        }

        if (!connectionPromise || (connection.state !== "Connected" && connection.state !== "Connecting")) {
            connectionPromise = connection
                .start()
                .then(function () {
                    console.log("SignalR connected");
                })
                .catch(function (err) {
                    console.error("Error starting SignalR connection:", err);
                    throw err;
                });
        }

        return connectionPromise;
    }

    async function init(onMessageReceived) {
        receiveHandler = onMessageReceived;
        await ensureConnection();
    }

    function onPresenceChanged(handler) {
        presenceHandler = handler;
    }

    function onMessageStatusChanged(handler) {
        messageStatusHandler = handler;
    }

    function onSessionEnded(handler) {
        sessionEndedHandler = handler;
    }

    function onUnreadCountChanged(handler) {
        unreadCountChangedHandler = handler;
    }

    async function joinChat(chatSessionId) {
        await ensureConnection();
        if (!chatSessionId) {
            throw new Error("chatSessionId is required");
        }
        currentSessionId = chatSessionId;

        if (joinedSessions.has(chatSessionId)) {
            return;
        }

        try {
            await connection.invoke("JoinChat", chatSessionId);
            joinedSessions.add(chatSessionId);
            console.log("Joined chat session:", chatSessionId);
        } catch (err) {
            console.error("Failed to join chat session:", err);
            throw err;
        }
    }

    async function leaveChat(chatSessionId) {
        if (!chatSessionId || !connection) return;
        try {
            await connection.invoke("LeaveChat", chatSessionId);
            joinedSessions.delete(chatSessionId);
            if (currentSessionId === chatSessionId) {
                currentSessionId = null;
            }
            console.log("Left chat session:", chatSessionId);
        } catch (err) {
            console.error("Failed to leave chat session:", err);
        }
    }

    async function stop() {
        if (!connection) return;
        try {
            await connection.stop();
        } catch (err) {
            console.warn("SignalR stop:", err);
        } finally {
            connection = null;
            connectionPromise = null;
            joinedSessions.clear();
            currentSessionId = null;
        }
    }

    async function initConnection(chatSessionId, onMessageReceived) {
        await init(onMessageReceived);
        await joinChat(chatSessionId);
    }

    async function sendMessageTo(chatSessionId, messageText, messageType = 'Text') {
        if (!chatSessionId) {
            throw new Error("chatSessionId is required");
        }
        currentSessionId = chatSessionId;
        await ensureConnection();

        try {
            const response = await fetch('/api/chat/send', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify({
                    chatSessionId: chatSessionId,
                    text: messageText,
                    messageType: messageType
                })
            });
            if (!response.ok) {
                const errorBody = await response.json();
                throw new Error(errorBody.message || 'Failed to send message.');
            }
            return response.json();
        } catch (err) {
            console.error("Failed to send message:", err);
            throw err;
        }
    }

    // Backward-compatible convenience (uses the last selected session)
    async function sendMessage(text) {
        if (!currentSessionId) {
            throw new Error("No active chat session selected.");
        }
        return sendMessageTo(currentSessionId, text);
    }

    return {
        init,
        initConnection,
        joinChat,
        leaveChat,
        stop,
        onPresenceChanged,
        onMessageStatusChanged,
        onSessionEnded,
        onUnreadCountChanged,
        joinAdminPresence: async function () {
            await ensureConnection();
            try {
                await connection.invoke("JoinAdminPresence");
            } catch (err) {
                console.error("Failed to join admin presence group:", err);
            }
        },
        sendMessageTo,
        sendMessage
    };
})();

