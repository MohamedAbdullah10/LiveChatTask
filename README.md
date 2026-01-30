# 💬 LiveChatTask

A robust, real-time Live Chat System built with ASP.NET Core 9. Enables seamless communication between Administrators and Users with advanced features including presence detection, rich media sharing, read receipts, and intelligent automation.

---

## 🛠 Tech Stack

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-Real--time-0078D4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/EF%20Core-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)

### Core Technologies
| Layer | Technology |
|-------|------------|
| **Framework** | ASP.NET Core 9 |
| **Real-time** | SignalR |
| **ORM** | Entity Framework Core 9 |
| **Database** | SQL Server |
| **Authentication** | ASP.NET Core Identity |
| **API Docs** | Swagger / OpenAPI |
| **Frontend** | Razor Pages + JavaScript |

### Architectural Patterns
- **Service Layer Pattern** — Business logic encapsulated in dedicated services
- **Background Services** — `IHostedService` for automated monitoring tasks
- **Repository Pattern** — Data access via EF Core DbContext
- **Real-time Messaging** — SignalR Hub for bidirectional communication

---

## ✨ Key Features

### 👤 User Module
| Feature | Description |
|---------|-------------|
| **Initiate Chat** | Users can open a new chat session with support |
| **Rich Messaging** | Send text, images, documents, and voice notes |
| **Read Receipts** | See when messages are delivered ("Sent") and read ("Seen") |
| **Session Timer** | Visual countdown showing remaining session time |
| **Auto-Termination Warning** | Receives system notification before idle timeout |

### 🛡️ Admin Module
| Feature | Description |
|---------|-------------|
| **Multi-Chat Dashboard** | Handle multiple user conversations simultaneously |
| **User Presence** | Real-time online/offline/idle status indicators |
| **Unread Badges** | Notification badges for new messages per user |
| **Session Management** | Open, monitor, and manage active chat sessions |
| **Configurable Limits** | Adjust max message length and session duration |

### ⚡ Real-time Features
| Feature | Description |
|---------|-------------|
| **SignalR Hub** | Instant message delivery to all connected clients |
| **Presence Broadcasting** | Admin dashboard receives live status updates |
| **Read Receipt Sync** | Status changes broadcast to all session participants |
| **Unread Count Updates** | Badge counts update in real-time |

### 🤖 Smart Automation
| Feature | Description |
|---------|-------------|
| **Idle Chat Monitor** | Background service checks for user inactivity every 30 seconds |
| **Auto-Termination** | After 1 minute of user silence, system sends: *"The chat will be terminated because we have not received a response from you."* |
| **Presence Monitor** | Detects and broadcasts online/offline status changes |

### 📎 Rich Media Support
| Type | Allowed Formats | Max Size |
|------|-----------------|----------|
| **Images** | JPG, PNG, GIF, WebP | 10 MB |
| **Documents** | PDF, DOC, DOCX, TXT | 10 MB |
| **Voice Notes** | WebM, OGG, MP4, WAV, M4A | 5 MB |

---

## 🏗 Architecture & Technical Highlights

```
LiveChatTask/
├── Controllers/          # API endpoints (Chat, Account, Presence, Settings)
├── Hubs/                 # SignalR ChatHub for real-time messaging
├── Services/             # Business logic layer
│   ├── ChatService       # Message persistence & session management
│   ├── PresenceService   # Online status tracking (thread-safe)
│   ├── FileUploadService # File validation & storage
│   ├── IdleChatMonitor   # BackgroundService for idle detection
│   └── PresenceMonitor   # BackgroundService for status broadcasting
├── Models/               # Entity models (ChatSession, Message, etc.)
├── Data/                 # EF Core DbContext & migrations
└── Pages/                # Razor Pages (Admin & User interfaces)
```

### Key Design Decisions

- **Thread-Safe Presence Tracking** — Uses `ConcurrentDictionary` for in-memory connection counts
- **Separation of Concerns** — Hub handles connection management only; business logic in services
- **Background Workers** — Two `IHostedService` implementations run independently:
  - `IdleChatMonitor` — Terminates inactive chats
  - `PresenceMonitor` — Broadcasts presence changes to admins
- **Configurable Settings** — Admin can adjust limits without code changes

---

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB, Express, or full instance)
- (Optional) [Visual Studio 2022](https://visualstudio.microsoft.com/) or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/LiveChatTask.git
   cd LiveChatTask
   ```

2. **Configure the database connection**
   
   Edit `appsettings.json` with your SQL Server connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LiveChatTask;Trusted_Connection=True;MultipleActiveResultSets=true"
     }
   }
   ```

3. **Apply database migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the application**
   - **User Portal:** `https://localhost:5001/`
   - **Admin Dashboard:** `https://localhost:5001/Admin`
   - **Swagger API:** `https://localhost:5001/swagger`

### Default Accounts
| Role | Email | Password |
|------|-------|----------|
| Admin | admin@chat.com | Admin123! |
| User | user@chat.com | User123! |

> ⚠️ **Note:** Change default credentials in production!

---

## 🔌 API Documentation

Interactive API documentation is available via **Swagger UI** at:

```
/swagger
```

### Key Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/chat/send` | POST | Send a message in a chat session |
| `/api/chat/my-session` | GET | Get or create user's active session |
| `/api/chat/sessions` | GET | Admin: List all user sessions |
| `/api/chat/history` | GET | Retrieve message history |
| `/api/chat/mark-seen` | POST | Mark messages as read |
| `/api/chat/upload-file` | POST | Upload image/document |
| `/api/chat/upload-voice` | POST | Upload voice recording |
| `/api/presence/heartbeat` | POST | Update user presence |
| `/api/settings/chat` | GET/POST | Get/Update chat settings |

---


## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

<p align="center">
  Built with ❤️ using ASP.NET Core 9
</p>
