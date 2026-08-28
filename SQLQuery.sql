USE master;
GO
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'SafeWayDB')
BEGIN
    ALTER DATABASE SafeWayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE SafeWayDB;
END;
GO

CREATE DATABASE SafeWayDB;
GO
USE SafeWayDB;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- CORE: users  (authentication + identity only)
CREATE TABLE users (
    id            INT IDENTITY(1,1) PRIMARY KEY,
    uniqueid      VARCHAR(20)   NOT NULL UNIQUE,
    password_hash VARCHAR(255)  NOT NULL,   -- store a bcrypt/argon2 hash, NEVER plaintext
    fullname      NVARCHAR(100) NOT NULL,
    role          VARCHAR(20)   NOT NULL CHECK (role IN ('Admin', 'Student', 'Parent', 'Driver')),
    phone         VARCHAR(20)   NULL,
    status        VARCHAR(20)   NOT NULL CONSTRAINT DF_users_status DEFAULT 'ACTIVE'
                      CHECK (status IN ('ACTIVE', 'INACTIVE', 'SUSPENDED')),
    createdat     DATETIME2(3)  NOT NULL CONSTRAINT DF_users_createdat DEFAULT SYSUTCDATETIME(),
    updatedat     DATETIME2(3)  NOT NULL CONSTRAINT DF_users_updatedat DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX idx_users_role   ON users (role);
CREATE INDEX idx_users_status ON users (status);
GO

-- REFERENCE DATA: stations, routes, buses
CREATE TABLE stations (
    id       INT IDENTITY(1,1) PRIMARY KEY,
    name     NVARCHAR(100) NOT NULL UNIQUE,
    isactive BIT           NOT NULL CONSTRAINT DF_stations_isactive DEFAULT 1
);
GO

CREATE TABLE routes (
    id       INT IDENTITY(1,1) PRIMARY KEY,
    name     NVARCHAR(100) NOT NULL UNIQUE,
    isactive BIT           NOT NULL CONSTRAINT DF_routes_isactive DEFAULT 1
);
GO

CREATE TABLE buses (
    id        INT IDENTITY(1,1) PRIMARY KEY,
    busnumber VARCHAR(20) NOT NULL UNIQUE,
    driverid  INT         NULL REFERENCES users(id)  ON DELETE NO ACTION,
    routeid   INT         NULL REFERENCES routes(id) ON DELETE NO ACTION,
    isactive  BIT         NOT NULL CONSTRAINT DF_buses_isactive DEFAULT 1
);
GO

CREATE INDEX idx_buses_driverid ON buses (driverid);
CREATE INDEX idx_buses_routeid  ON buses (routeid);
-- Filtered unique index: at most one active bus per driver, but any
-- number of buses can have driverid = NULL (unassigned).
CREATE UNIQUE INDEX uq_buses_driverid ON buses (driverid) WHERE driverid IS NOT NULL;
GO


-- ROLE PROFILES: students, drivers
CREATE TABLE students (
    id        INT IDENTITY(1,1) PRIMARY KEY,
    userid    INT         NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    parentid  INT         NULL REFERENCES users(id)    ON DELETE NO ACTION,
    grade     VARCHAR(20) NULL,
    busid     INT         NULL REFERENCES buses(id)    ON DELETE NO ACTION,
    stationid INT         NULL REFERENCES stations(id) ON DELETE NO ACTION
);
GO

CREATE INDEX idx_students_parentid  ON students (parentid);
CREATE INDEX idx_students_busid     ON students (busid);
CREATE INDEX idx_students_stationid ON students (stationid);
GO

CREATE TABLE drivers (
    id     INT IDENTITY(1,1) PRIMARY KEY,
    userid INT NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE
);
GO

-- =====================================================================
-- SUBSCRIPTIONS
-- =====================================================================
CREATE TABLE subscriptions (
    id        INT IDENTITY(1,1) PRIMARY KEY,
    userid    INT          NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    status    VARCHAR(20)  NOT NULL CONSTRAINT DF_subscriptions_status DEFAULT 'UNPAID'
                  CHECK (status IN ('PAID', 'UNPAID', 'CANCELLED')),
    startdate DATE         NOT NULL,
    enddate   DATE         NOT NULL,
    createdat DATETIME2(3) NOT NULL CONSTRAINT DF_subscriptions_createdat DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_subscriptions_daterange CHECK (enddate >= startdate)
);
GO

CREATE INDEX idx_subscriptions_userid    ON subscriptions (userid);
CREATE INDEX idx_subscriptions_status    ON subscriptions (status);
CREATE INDEX idx_subscriptions_daterange ON subscriptions (startdate, enddate);
GO
-- Note: SQL Server has no EXCLUDE constraint like PostgreSQL. To block
-- overlapping subscription date ranges per user, enforce it in a
-- trigger or in application logic before insert.

-- =====================================================================
-- ROUTES <-> STATIONS (stop schedule)
-- =====================================================================
CREATE TABLE routestations (
    id         INT IDENTITY(1,1) PRIMARY KEY,
    routeid    INT   NOT NULL REFERENCES routes(id)   ON DELETE CASCADE,
    stationid  INT   NOT NULL REFERENCES stations(id) ON DELETE NO ACTION,
    stoporder  INT   NOT NULL CHECK (stoporder > 0),
    pickuptime TIME(0) NOT NULL,
    CONSTRAINT UQ_routestations_order   UNIQUE (routeid, stoporder),
    CONSTRAINT UQ_routestations_station UNIQUE (routeid, stationid)
);
GO

CREATE INDEX idx_routestations_routeid   ON routestations (routeid);
CREATE INDEX idx_routestations_stationid ON routestations (stationid);
GO

-- =====================================================================
-- CHANGE REQUESTS
-- =====================================================================
CREATE TABLE stationchangerequests (
    id            INT IDENTITY(1,1) PRIMARY KEY,
    userid        INT           NOT NULL REFERENCES users(id)    ON DELETE CASCADE,
    newstationid  INT           NOT NULL REFERENCES stations(id) ON DELETE NO ACTION,
    effectivedate DATE          NULL,
    status        VARCHAR(20)   NOT NULL CONSTRAINT DF_scr_status DEFAULT 'PENDING'
                      CHECK (status IN ('PENDING', 'APPROVED', 'REJECTED')),
    adminnote     NVARCHAR(255) NULL,
    createdat     DATETIME2(3)  NOT NULL CONSTRAINT DF_scr_createdat DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX idx_scr_userid       ON stationchangerequests (userid);
CREATE INDEX idx_scr_newstationid ON stationchangerequests (newstationid);
CREATE INDEX idx_scr_status       ON stationchangerequests (status);
GO

CREATE TABLE routechangerequests (
    id            INT IDENTITY(1,1) PRIMARY KEY,
    userid        INT           NOT NULL REFERENCES users(id)    ON DELETE CASCADE,
    newstationid  INT           NOT NULL REFERENCES stations(id) ON DELETE NO ACTION,
    newrouteid    INT           NOT NULL REFERENCES routes(id)   ON DELETE NO ACTION,
    effectivedate DATE          NOT NULL,
    status        VARCHAR(20)   NOT NULL CONSTRAINT DF_rcr_status DEFAULT 'PENDING'
                      CHECK (status IN ('PENDING', 'APPROVED', 'REJECTED')),
    adminnote     NVARCHAR(255) NULL,
    createdat     DATETIME2(3)  NOT NULL CONSTRAINT DF_rcr_createdat DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX idx_rcr_userid       ON routechangerequests (userid);
CREATE INDEX idx_rcr_newstationid ON routechangerequests (newstationid);
CREATE INDEX idx_rcr_newrouteid   ON routechangerequests (newrouteid);
CREATE INDEX idx_rcr_status       ON routechangerequests (status);
GO

-- =====================================================================
-- updatedat AUTO-MAINTENANCE
-- =====================================================================
CREATE TRIGGER trg_users_updatedat
ON users
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE u
    SET updatedat = SYSUTCDATETIME()
    FROM users u
    INNER JOIN inserted i ON u.id = i.id;
END;
GO

-- =====================================================================
-- SEED DATA
-- =====================================================================
INSERT INTO users (uniqueid, password_hash, fullname, role) VALUES
    ('Admin', 'Admin123', N'System Admin', 'Admin');

INSERT INTO users (uniqueid, password_hash, fullname, role) VALUES
    ('DRV001', 'Driver123', N'Khalid Hassan', 'Driver');
INSERT INTO drivers (userid) SELECT id FROM users WHERE uniqueid = 'DRV001';

INSERT INTO users (uniqueid, password_hash, fullname, role) VALUES
    ('PAR001', 'Parent123', N'Mohammed Ali', 'Parent');

INSERT INTO stations (name) VALUES
    (N'Main Street Station'), (N'Park Avenue Station'), (N'Broadway Station'),
    (N'Downtown Station'), (N'North Gate Station');

INSERT INTO routes (name) VALUES
    (N'Route A - Downtown'), (N'Route B - North Side'), (N'Route C - East District'),
    (N'Route D - West End'), (N'Route E - South Gate');

INSERT INTO buses (busnumber, driverid, routeid)
    SELECT 'BUS-101', u.id, r.id
    FROM users u, routes r
    WHERE u.uniqueid = 'DRV001' AND r.name = N'Route A - Downtown';

INSERT INTO users (uniqueid, password_hash, fullname, role) VALUES
    ('STU001', 'Student123', N'Ahmed Ali', 'Student');

INSERT INTO students (userid, parentid, grade, busid, stationid)
    SELECT s.id, p.id, 'Grade 10', b.id, st.id
    FROM users s, users p, buses b, stations st
    WHERE s.uniqueid = 'STU001' AND p.uniqueid = 'PAR001'
      AND b.busnumber = 'BUS-101' AND st.name = N'Main Street Station';

INSERT INTO subscriptions (userid, status, startdate, enddate)
    SELECT id, 'PAID', '2026-03-01', '2026-03-31' FROM users WHERE uniqueid = 'PAR001'
    UNION ALL
    SELECT id, 'UNPAID', '2026-03-01', '2026-03-31' FROM users WHERE uniqueid = 'STU001';

INSERT INTO routestations (routeid, stationid, stoporder, pickuptime) VALUES
    (1, 1, 1, '07:15'), (1, 2, 2, '07:25'), (1, 3, 3, '07:35'),
    (2, 4, 1, '07:10'), (2, 5, 2, '07:20'),
    (3, 1, 1, '07:30'), (3, 3, 2, '07:45'),
    (4, 2, 1, '07:00'), (4, 4, 2, '07:15'),
    (5, 5, 1, '07:05'), (5, 1, 2, '07:20');
GO

-- =====================================================================
-- VERIFICATION QUERIES
-- =====================================================================
SELECT 'users'                AS TableName, COUNT(*) AS Rows FROM users
UNION ALL SELECT 'students',              COUNT(*) FROM students
UNION ALL SELECT 'drivers',               COUNT(*) FROM drivers
UNION ALL SELECT 'buses',                 COUNT(*) FROM buses
UNION ALL SELECT 'subscriptions',         COUNT(*) FROM subscriptions
UNION ALL SELECT 'stations',              COUNT(*) FROM stations
UNION ALL SELECT 'routes',                COUNT(*) FROM routes
UNION ALL SELECT 'routestations',         COUNT(*) FROM routestations
UNION ALL SELECT 'stationchangerequests', COUNT(*) FROM stationchangerequests
UNION ALL SELECT 'routechangerequests',   COUNT(*) FROM routechangerequests;
GO

SELECT
    r.name        AS Route,
    rs.stoporder,
    s.name        AS Station,
    rs.pickuptime
FROM routestations rs
JOIN routes   r ON r.id = rs.routeid
JOIN stations s ON s.id = rs.stationid
ORDER BY r.id, rs.stoporder;
GO

SELECT
    u.fullname   AS Student,
    st.grade,
    b.busnumber,
    d.fullname   AS Driver,
    stn.name     AS Station,
    p.fullname   AS Parent
FROM students st
JOIN users u          ON u.id = st.userid
LEFT JOIN buses b      ON b.id = st.busid
LEFT JOIN users d      ON d.id = b.driverid
LEFT JOIN stations stn ON stn.id = st.stationid
LEFT JOIN users p      ON p.id = st.parentid;
GO