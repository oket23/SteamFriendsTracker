# Steam Friends Tracker 🎮

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)
![Docker](https://img.shields.io/badge/Docker-Enabled-2496ED?style=flat-square&logo=docker)
![Architecture](https://img.shields.io/badge/Architecture-Microservices-orange?style=flat-square)

A robust, full-stack observability platform designed to aggregate, normalize, and track Steam user activity in real-time. Built on a scalable **Microservices Architecture** using **.NET 9** and **React**, featuring Gateway Offloading and Event-Driven updates via SignalR.

---

### 📸 Dashboard Preview


<img width="1301" height="573" alt="example" src="https://github.com/user-attachments/assets/3f452f3d-770a-4416-9805-8a3f2ba61caa" />
> *Real-time dashboard showing friends' status, game library aggregation, and profile statistics.*

---

## 🏗 High-Level Architecture

The system implements the **API Gateway** pattern to centralize routing, authentication, and SSL termination. It leverages **SignalR** for pushing live state changes (Online/Offline/In-Game) directly to the client, bypassing the need for aggressive polling from the frontend.

### Service Registry & Ports

| Service | Docker Port | Localhost Port | Description |
| :--- | :--- | :--- | :--- |
| **🌐 Gateway.Api** | `5001` | `5001` | **Entry Point (YARP).** Routes all HTTP/WS traffic. Handles CORS & Auth. |
| **🔐 Auth.Api** | `5100` | `5100` | **Identity Provider.** Steam OpenID 2.0 auth & JWT issuance. |
| **⚡ Friends.Api** | `5200` | `5200` | **Real-time Core.** SignalR Hub & Background Workers. |
| **🎮 Game.Api** | `5300` | `5300` | **Data Aggregator.** Steam Store Proxy with Redis caching. |
| **Redis** | `6379` | `6379` | Distributed Caching layer. |
| **PostgreSQL** | `5432` | `5432` | Persistent storage for User Identity. |

## 🚀 Key Technical Features

* **Gateway Offloading:** Internal microservices are isolated from the public internet. All traffic is sanitized and authenticated via **YARP** (Yet Another Reverse Proxy).
* **Real-Time Event Model:** Uses a `HostedService` background worker to poll Steam APIs and push differentials (state changes) via **SignalR** websockets instantly.
* **Performance Optimization:** Aggressive **Redis** caching strategies to minimize latency and respect Steam API rate limits.
* **Secure Authentication:** Implementation of **OpenID 2.0** for Steam integration, exchanged for internal secure **JWT** (Access/Refresh tokens).
* **Infrastructure as Code:** Fully containerized environment using **Docker Compose** for consistent orchestration across dev/prod environments.

## 🛠 Tech Stack

### Backend (.NET 9)
* **Core:** ASP.NET Core Web API
* **Proxy:** YARP
* **Real-Time:** SignalR
* **Data:** Entity Framework Core (PostgreSQL)
* **Caching:** Redis
* **Logging:** Serilog (Structured Logging)

## 🔌 API Examples

Access via Gateway (Port 5001):

```http
# 1. Get Current User Profile
GET /auth-api/steam/me
Authorization: Bearer <jwt_token>

# 2. Search Games (Cached Proxy)
GET /games-api/steam/search?term=Counter-Strike&lang=uk
Authorization: Bearer <jwt_token>

# 3. WebSocket Connection (SignalR)
wss://localhost:5001/friends-api/hubs/friends?access_token=<jwt_token>
```
## 🚀 Getting Started

### Prerequisites
* [Docker Desktop](https://www.docker.com/products/docker-desktop/)
* A valid [Steam API Key](https://steamcommunity.com/dev/apikey)

### 1. Environment Setup
Create a `.env` file in the `SteamFriensTracker` root directory (next to `docker-compose.yml`):

```ini
# Database
POSTGRES_DB=steam_db
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_secure_password

# Steam API (Required)
STEAM_API_KEY=YOUR_STEAM_API_KEY_HERE

# Infrastructure
REDIS_CONNECTION=redis:6379
```

### 2. Run with Docker

Execute the following command to orchestrate the entire fleet:
```bash
docker-compose up --build
```

### 3. Access

Once the containers are running, the services will be available through the Gateway.


* **Swagger UI (Unified Documentation):**

    Open [https://localhost:5001/swagger](https://localhost:5001/swagger) in your browser.

    *Note: This aggregates endpoints from Auth, Friends, and Game services into one interface.*



* **API Gateway URL:** `https://localhost:5001`



*(⚠️ **Important:** Your browser will likely warn you about the connection not being private because the project uses a self-signed development certificate. You must manually click "Advanced" -> "Proceed to localhost (unsafe)" to access the Swagger UI).*



### 4. Stopping the Services

To stop all running containers and free up ports, run:



```bash
docker-compose down
```
## 💡 Why This Project Matters

This project demonstrates:
- Real microservices communication with internal isolation.
- Reverse proxy & offloading patterns using YARP.
- Real-time data flow using differential updates (not polling).
- Combined use of Redis + PostgreSQL as separate infra layers.
- Steam OpenID integration and secure token exchange.
- Production-grade Docker orchestration for multiple services.

## 🐛 Troubleshooting & Common Issues

If you encounter errors during startup, try these solutions:

1.  **"Connection Refused" to Database:**
    * *Symptom:* API logs show errors connecting to `postgres`.
    * *Cause:* The database container takes longer to start than the API containers.
    * *Fix:* Simply restart the API containers:
        ```bash
          docker-compose restart
        ```

2.  **SSL/Certificate Errors:**
    * *Symptom:* Browser shows "Not Secure" or Docker complains about mounting volumes.
    * *Fix:* Ensure you have generated and trusted the developer certificate:
        ```bash
          dotnet dev-certs https -ep ./certs/gateway.pfx -p your_secure_password
        ```
        *Make sure the password in your `.env` matches the one used here.*

3.  **SignalR Connection Failed (CORS/401):**
    * *Symptom:* Frontend console shows WebSocket errors.
    * *Fix:*
        * Ensure you are connecting via the **Gateway port (5001)**.
        * Check that `STEAM_API_KEY` is set correctly in `.env`.
        * Verify the Gateway is running and routing traffic (`https://localhost:5001/swagger` should work).

## 📂 Project Structure

A high-level overview of the backend solution:

```text
SteamFriensTracker/
├── Auth.Api/           # Authentication Service (Identity, JWT, Postgres DB)
├── Friends.Api/        # Friends Business Logic & SignalR Hub (Real-time)
├── Game.Api/           # Steam Store Data Proxy & Redis Caching
├── Gateway.Api/        # YARP Reverse Proxy (The only public entry point)
├── Shared/             # Shared Projects (DTOs, Models, Enums)
├── certs/              # SSL certificates for secure Docker communication
├── docker-compose.yml  # Orchestration for APIs, Redis, and Postgres
└── .env                # Environment variables and secrets
```

## 🔐 Security Features

* **Gateway Offloading:** Internal microservices (`Auth`, `Friends`, `Game`) are not exposed directly to the public internet. All HTTP and WebSocket traffic is sanitized and routed through the **Gateway.Api**.
* **Secure WebSockets:** SignalR connections use a secure token transmission mechanism via Query String (`?access_token=...`). This token is intercepted and validated by the Gateway before the connection is allowed to reach the Hub.
* **Secret Management:** Sensitive keys (Database passwords, Steam API Keys, Connection Strings) are strictly managed via environment variables (`.env`) and are **never** hardcoded in the source code.

## 📜 License

This project is licensed under the MIT License. Feel free to use it for educational purposes.
