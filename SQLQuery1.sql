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



INSERT INTO Users (UniqueID, Password, FullName, Role)
VALUES ('Admin', 'Admin123', 'System Admin', 'Admin');

INSERT INTO Users (UniqueID, Password, FullName, Role, Grade)
VALUES ('STU001', 'stu001pass', 'Ahmed Ali', 'Student', 'Grade 10');


INSERT INTO Users (UniqueID, Password, FullName, Role)
VALUES ('PAR001', 'par001pass', 'Mohammed Ali', 'Parent');


INSERT INTO Users (UniqueID, Password, FullName, Role)
VALUES ('DRV001', 'drv001pass', 'Khalid Hassan', 'Driver');
GO

ALTER TABLE Users ADD ParentId INT NULL;


UPDATE Users SET ParentId = 3 WHERE UniqueID = 'STU001';


INSERT INTO Subscriptions (UserId, Status, StartDate, EndDate)
VALUES (2, 'PAID', '2026-03-01', '2026-03-31');

INSERT INTO Subscriptions (UserId, Status, StartDate, EndDate)
VALUES (3, 'UNPAID', '2026-03-01', '2026-03-31');