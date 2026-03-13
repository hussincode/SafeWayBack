# 🚌 SafeWay API — Smart School Bus System

<div align="center">

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-000000?style=for-the-badge&logo=jsonwebtokens&logoColor=white)
![BCrypt](https://img.shields.io/badge/BCrypt-Secured-green?style=for-the-badge)

> **Real-time tracking and notifications for safe, organized school transportation.**

</div>

---

## 📖 About

SafeWay is a smart school bus management system built for students, parents, drivers, and admins. This repository contains the **backend REST API** built with **.NET** and **SQL Server**.

The mobile app (Flutter) connects to this API to handle login, bus tracking, notifications, and more.

---

## 🏗️ Project Structure

```
SafeWayAPI/
├── Controllers/        # API endpoints (Auth, Bus, etc.)
├── Data/               # Database context (EF Core)
├── DTOs/               # Data Transfer Objects
├── Helpers/            # Utility classes
├── Models/             # Database models
├── appsettings.json    # Config (not in repo - see below)
└── Program.cs          # App entry point
```

---

## 👥 Roles

| Role    | Description                              | Account Creation     |
|---------|------------------------------------------|----------------------|
| 🧑‍🎓 Student | Track bus, view routes, get notifications | Self register        |
| 👨‍👩‍👧 Parent  | Monitor children, receive boarding alerts | Self register        |
| 🚌 Driver  | Manage routes, confirm boardings          | Created by Admin only |
| 🔐 Admin   | Full system control                       | Pre-configured       |

---

## 🔐 Authentication

- Uses **JWT Tokens** for secure authentication
- Passwords are hashed with **BCrypt**
- Every request after login requires the token in the header
- Tokens expire after **7 days**

---

## 🛠️ Tech Stack

| Layer       | Technology              |
|-------------|-------------------------|
| Framework   | ASP.NET Core 8          |
| Database    | SQL Server (LocalDB)    |
| ORM         | Entity Framework Core   |
| Auth        | JWT Bearer Tokens       |
| Security    | BCrypt Password Hashing |
| API Docs    | Swagger / OpenAPI       |

---

## 🚀 Getting Started

### 1. Clone the repo

```bash
git clone https://github.com/hussincode/SafeWayBack.git
cd SafeWayBack
```

### 2. Install dependencies

```bash
dotnet restore
```

### 3. Set up the database

Open **SQL Server Management Studio** and run:

```sql
CREATE DATABASE SafeWayDB;
GO

USE SafeWayDB;
GO

CREATE TABLE Users (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    UniqueID    NVARCHAR(20)  NOT NULL UNIQUE,  
    Password    NVARCHAR(255) NOT NULL,          
    FullName    NVARCHAR(100) NOT NULL,
    Role        NVARCHAR(20)  NOT NULL,          
    Grade       NVARCHAR(20)  NULL,              
    CreatedAt   DATETIME DEFAULT GETDATE()
);
GO


INSERT INTO Users (UniqueID, Password, FullName, Role)
VALUES ('Admin', 'Admin123', 'System Admin', 'Admin');

INSERT INTO Users (UniqueID, Password, FullName, Role, Grade)
VALUES ('STU001', 'stu001pass', 'Ahmed Ali', 'Student', 'Grade 10');


INSERT INTO Users (UniqueID, Password, FullName, Role)
VALUES ('PAR001', 'par001pass', 'Mohammed Ali', 'Parent');


INSERT INTO Users (UniqueID, Password, FullName, Role)
VALUES ('DRV001', 'drv001pass', 'Khalid Hassan', 'Driver');
GO
```

### 4. Create `appsettings.json`

Copy the example file and fill in your values:

```bash
cp appsettings.example.json appsettings.json
```

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=SafeWayDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "SafeWaySecretKey2026SuperLongKeyForJWT!",
    "Issuer": "SafeWayAPI",
    "Audience": "SafeWayApp",
    "ExpiryDays": 7
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5143"
      }
    }
  },
  "AllowedHosts": "*"
}
```

### 5. Hash the default passwords

Run the project and call this endpoint once:

```
GET http://localhost:5143/api/auth/setup
```

You should see: `Passwords hashed!`

> ⚠️ Delete the `/setup` endpoint after using it in production!

### 6. Run the API

```bash
dotnet run
```

API will be available at:
```
http://localhost:5143
```

---

## 📡 API Endpoints

### Auth

| Method | Endpoint              | Description         | Auth Required |
|--------|-----------------------|---------------------|---------------|
| POST   | `/api/auth/login`     | Login for all roles | ❌            |
| GET    | `/api/auth/setup`     | Hash passwords once | ❌            |

### Login Request Example

```json
POST /api/auth/login
{
  "uniqueID": "PAR001",
  "password": "par001pass"
}
```

### Login Response Example

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "fullName": "Mohammed Ali",
  "role": "Parent",
  "uniqueID": "PAR001"
}
```

---

## 🔗 Connected Mobile App

This API is built to work with the **SafeWay Flutter App**:

👉 [App Repository](https://github.com/hussincode/App_SafeWay)

The app connects to this API using the device's local network IP address.

---

## 👨‍💻 Developer

Built by **SafeWay Team** — Computer Science Capstone Project

---

## 📄 License

This project is for educational purposes as part of a capstone project.
