CREATE DATABASE SafeWayDB;
GO

USE SafeWayDB;
GO



CREATE TABLE Users (
    Id         INT          IDENTITY(1,1) PRIMARY KEY,
    UniqueID   NVARCHAR(20)  NOT NULL UNIQUE,
    Password   NVARCHAR(255) NOT NULL,
    FullName   NVARCHAR(100) NOT NULL,
    Role       NVARCHAR(20)  NOT NULL,
    Grade      NVARCHAR(20)  NULL,
    ParentId   INT           NULL,
    BusNumber  NVARCHAR(20)  NULL,
    DriverName NVARCHAR(100) NULL,
    RouteName  NVARCHAR(100) NULL,
    StopName   NVARCHAR(100) NULL,
    CreatedAt  DATETIME      DEFAULT GETDATE()
);
GO

CREATE TABLE Subscriptions (
    Id        INT          IDENTITY(1,1) PRIMARY KEY,
    UserId    INT          NOT NULL,
    Status    NVARCHAR(20) NOT NULL DEFAULT 'UNPAID',
    StartDate DATE         NOT NULL,
    EndDate   DATE         NOT NULL,
    CreatedAt DATETIME     DEFAULT GETDATE(),
    CONSTRAINT FK_Subscriptions_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);
GO

CREATE TABLE Stations (
    Id       INT          IDENTITY(1,1) PRIMARY KEY,
    Name     NVARCHAR(100) NOT NULL,
    IsActive BIT           DEFAULT 1
);
GO

CREATE TABLE Routes (
    Id       INT          IDENTITY(1,1) PRIMARY KEY,
    Name     NVARCHAR(100) NOT NULL,
    IsActive BIT           DEFAULT 1
);
GO

CREATE TABLE StationChangeRequests (
    Id            INT           IDENTITY(1,1) PRIMARY KEY,
    UserId        INT           NOT NULL,
    NewStationId  INT           NOT NULL,
    EffectiveDate DATE          NULL,
    Status        NVARCHAR(20)  NOT NULL DEFAULT 'PENDING',
    AdminNote     NVARCHAR(255) NULL,
    CreatedAt     DATETIME      DEFAULT GETDATE(),
    CONSTRAINT FK_SCR_Users    FOREIGN KEY (UserId)       REFERENCES Users(Id),
    CONSTRAINT FK_SCR_Stations FOREIGN KEY (NewStationId) REFERENCES Stations(Id)
);
GO

CREATE TABLE RouteChangeRequests (
    Id            INT           IDENTITY(1,1) PRIMARY KEY,
    UserId        INT           NOT NULL,
    NewStationId  INT           NOT NULL,
    NewRouteId    INT           NOT NULL,
    EffectiveDate DATE          NOT NULL,
    Status        NVARCHAR(20)  NOT NULL DEFAULT 'PENDING',
    AdminNote     NVARCHAR(255) NULL,
    CreatedAt     DATETIME      DEFAULT GETDATE(),
    CONSTRAINT FK_RCR_Users    FOREIGN KEY (UserId)      REFERENCES Users(Id),
    CONSTRAINT FK_RCR_Stations FOREIGN KEY (NewStationId) REFERENCES Stations(Id),
    CONSTRAINT FK_RCR_Routes   FOREIGN KEY (NewRouteId)  REFERENCES Routes(Id)
);
GO

CREATE TABLE RouteStations (
    Id           INT          IDENTITY(1,1) PRIMARY KEY,
    RouteId      INT          NOT NULL,
    StationId    INT          NOT NULL,
    StopOrder    INT          NOT NULL,
    PickupTime   NVARCHAR(10) NOT NULL,
    CONSTRAINT FK_RS_Routes   FOREIGN KEY (RouteId)   REFERENCES Routes(Id),
    CONSTRAINT FK_RS_Stations FOREIGN KEY (StationId) REFERENCES Stations(Id)
);
GO



INSERT INTO Users (UniqueID, Password, FullName, Role)
VALUES ('Admin', 'Admin123', 'System Admin', 'Admin');

INSERT INTO Users (UniqueID, Password, FullName, Role, Grade, BusNumber, DriverName, RouteName, StopName)
VALUES ('STU001', 'stu001pass', 'Ahmed Ali', 'Student', 'Grade 10', 'BUS-101', 'Khalid Hassan', 'Route A - Downtown', 'Main Street Station');

INSERT INTO Users (UniqueID, Password, FullName, Role)
VALUES ('PAR001', 'par001pass', 'Mohammed Ali', 'Parent');

INSERT INTO Users (UniqueID, Password, FullName, Role)
VALUES ('DRV001', 'drv001pass', 'Khalid Hassan', 'Driver');
GO

UPDATE Users SET ParentId = 3 WHERE UniqueID = 'STU001';
GO

INSERT INTO Subscriptions (UserId, Status, StartDate, EndDate) VALUES
    (2, 'PAID',   '2026-03-01', '2026-03-31'),
    (3, 'UNPAID', '2026-03-01', '2026-03-31');
GO

INSERT INTO Stations (Name) VALUES
    ('Main Street Station'),
    ('Park Avenue Station'),
    ('Broadway Station'),
    ('Downtown Station'),
    ('North Gate Station');
GO

INSERT INTO Routes (Name) VALUES
    ('Route A - Downtown'),
    ('Route B - North Side'),
    ('Route C - East District'),
    ('Route D - West End'),
    ('Route E - South Gate');
GO



INSERT INTO RouteStations (RouteId, StationId, StopOrder, PickupTime) VALUES
    (1, 1, 1, '07:15 AM'),   
    (1, 2, 2, '07:25 AM'),   
    (1, 3, 3, '07:35 AM');   


INSERT INTO RouteStations (RouteId, StationId, StopOrder, PickupTime) VALUES
    (2, 4, 1, '07:10 AM'),   
    (2, 5, 2, '07:20 AM');   


INSERT INTO RouteStations (RouteId, StationId, StopOrder, PickupTime) VALUES
    (3, 1, 1, '07:30 AM'),
    (3, 3, 2, '07:45 AM'),
    (4, 2, 1, '07:00 AM'),
    (4, 4, 2, '07:15 AM'),
    (5, 5, 1, '07:05 AM'),
    (5, 1, 2, '07:20 AM');
GO



SELECT 'Users'                AS TableName, COUNT(*) AS Rows FROM Users
UNION ALL
SELECT 'Subscriptions',                     COUNT(*) FROM Subscriptions
UNION ALL
SELECT 'Stations',                          COUNT(*) FROM Stations
UNION ALL
SELECT 'Routes',                            COUNT(*) FROM Routes
UNION ALL
SELECT 'StationChangeRequests',             COUNT(*) FROM StationChangeRequests
UNION ALL
SELECT 'RouteChangeRequests',               COUNT(*) FROM RouteChangeRequests;
GO

SELECT
    r.Name        AS Route,
    rs.StopOrder,
    s.Name        AS Station,
    rs.PickupTime
FROM RouteStations rs
JOIN Routes   r ON r.Id = rs.RouteId
JOIN Stations s ON s.Id = rs.StationId
ORDER BY r.Id, rs.StopOrder;
GO