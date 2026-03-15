
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
    ParentId    INT           NULL,
    BusNumber   NVARCHAR(20)  NULL,
    DriverName  NVARCHAR(100) NULL,
    RouteName   NVARCHAR(100) NULL,
    StopName    NVARCHAR(100) NULL,
    CreatedAt   DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE Subscriptions (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    UserId    INT NOT NULL,
    Status    NVARCHAR(20) NOT NULL DEFAULT 'UNPAID',
    StartDate DATE NOT NULL,
    EndDate   DATE NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Subscriptions_Users
        FOREIGN KEY (UserId) REFERENCES Users(Id)
);
GO

CREATE TABLE Stations (
    Id       INT IDENTITY(1,1) PRIMARY KEY,
    Name     NVARCHAR(100) NOT NULL,
    IsActive BIT DEFAULT 1
);
GO

CREATE TABLE StationChangeRequests (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    UserId        INT NOT NULL,
    NewStationId  INT NOT NULL,
    EffectiveDate DATE NULL,
    Status        NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    CreatedAt     DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_SCR_Users    FOREIGN KEY (UserId)      REFERENCES Users(Id),
    CONSTRAINT FK_SCR_Stations FOREIGN KEY (NewStationId) REFERENCES Stations(Id)
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



INSERT INTO Subscriptions (UserId, Status, StartDate, EndDate)
VALUES (2, 'PAID', '2026-03-01', '2026-03-31');

INSERT INTO Subscriptions (UserId, Status, StartDate, EndDate)
VALUES (3, 'UNPAID', '2026-03-01', '2026-03-31');
GO


INSERT INTO Stations (Name) VALUES
    ('Main Street Station'),
    ('Park Avenue Station'),
    ('Broadway Station'),
    ('Downtown Station'),
    ('North Gate Station');
GO


SELECT 'Users'                 AS TableName, COUNT(*) AS Rows FROM Users
UNION ALL
SELECT 'Subscriptions',                      COUNT(*) FROM Subscriptions
UNION ALL
SELECT 'Stations',                           COUNT(*) FROM Stations
UNION ALL
SELECT 'StationChangeRequests',              COUNT(*) FROM StationChangeRequests;
GO