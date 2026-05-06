-- PostgreSQL version of SafeWayDB schema
-- Note: Using lowercase table names as per PostgreSQL convention

-- CREATE DATABASE safewaydb;
-- (Database should already exist)

CREATE TABLE users (
    id         SERIAL       PRIMARY KEY,
    uniqueid   VARCHAR(20)  NOT NULL UNIQUE,
    password   VARCHAR(255) NOT NULL,
    fullname   VARCHAR(100) NOT NULL,
    role       VARCHAR(20)  NOT NULL,
    phone      VARCHAR(20)  NULL,
    status     VARCHAR(20)  NULL,
    grade      VARCHAR(20)  NULL,
    parentid   INTEGER      NULL,
    busnumber  VARCHAR(20)  NULL,
    drivername VARCHAR(100) NULL,
    routename  VARCHAR(100) NULL,
    stopname   VARCHAR(100) NULL,
    createdat  TIMESTAMP    DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE subscriptions (
    id        SERIAL       PRIMARY KEY,
    userid    INTEGER      NOT NULL,
    status    VARCHAR(20)  NOT NULL DEFAULT 'UNPAID',
    startdate DATE         NOT NULL,
    enddate   DATE         NOT NULL,
    createdat TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_Subscriptions_Users FOREIGN KEY (userid) REFERENCES users(id)
);

CREATE TABLE stations (
    id       SERIAL       PRIMARY KEY,
    name     VARCHAR(100) NOT NULL,
    isactive BOOLEAN      DEFAULT TRUE
);

CREATE TABLE routes (
    id       SERIAL       PRIMARY KEY,
    name     VARCHAR(100) NOT NULL,
    isactive BOOLEAN      DEFAULT TRUE
);

CREATE TABLE stationchangerequests (
    id            SERIAL        PRIMARY KEY,
    userid        INTEGER       NOT NULL,
    newstationid  INTEGER       NOT NULL,
    effectivedate DATE          NULL,
    status        VARCHAR(20)   NOT NULL DEFAULT 'PENDING',
    adminnote     VARCHAR(255)  NULL,
    createdat     TIMESTAMP     DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_SCR_Users    FOREIGN KEY (userid)       REFERENCES users(id),
    CONSTRAINT FK_SCR_Stations FOREIGN KEY (newstationid) REFERENCES stations(id)
);

CREATE TABLE routechangerequests (
    id            SERIAL        PRIMARY KEY,
    userid        INTEGER       NOT NULL,
    newstationid  INTEGER       NOT NULL,
    newrouteid    INTEGER       NOT NULL,
    effectivedate DATE          NOT NULL,
    status        VARCHAR(20)   NOT NULL DEFAULT 'PENDING',
    adminnote     VARCHAR(255)  NULL,
    createdat     TIMESTAMP     DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_RCR_Users    FOREIGN KEY (userid)      REFERENCES users(id),
    CONSTRAINT FK_RCR_Stations FOREIGN KEY (newstationid) REFERENCES stations(id),
    CONSTRAINT FK_RCR_Routes   FOREIGN KEY (newrouteid)  REFERENCES routes(id)
);

CREATE TABLE routestations (
    id         SERIAL       PRIMARY KEY,
    routeid    INTEGER      NOT NULL,
    stationid  INTEGER      NOT NULL,
    stoporder  INTEGER      NOT NULL,
    pickuptime VARCHAR(10)  NOT NULL,
    CONSTRAINT FK_RS_Routes   FOREIGN KEY (routeid)   REFERENCES routes(id),
    CONSTRAINT FK_RS_Stations FOREIGN KEY (stationid) REFERENCES stations(id)
);

INSERT INTO users (uniqueid, password, fullname, role)
VALUES ('Admin', 'Admin123', 'System Admin', 'Admin');

INSERT INTO users (uniqueid, password, fullname, role, grade, busnumber, drivername, routename, stopname)
VALUES ('STU001', 'stu001pass', 'Ahmed Ali', 'Student', 'Grade 10', 'BUS-101', 'Khalid Hassan', 'Route A - Downtown', 'Main Street Station');

INSERT INTO users (uniqueid, password, fullname, role)
VALUES ('PAR001', 'par001pass', 'Mohammed Ali', 'Parent');

INSERT INTO users (uniqueid, password, fullname, role)
VALUES ('DRV001', 'drv001pass', 'Khalid Hassan', 'Driver');

UPDATE users SET parentid = 3 WHERE uniqueid = 'STU001';

INSERT INTO subscriptions (userid, status, startdate, enddate) VALUES
    (2, 'PAID',   '2026-03-01', '2026-03-31'),
    (3, 'UNPAID', '2026-03-01', '2026-03-31');

INSERT INTO stations (name) VALUES
    ('Main Street Station'),
    ('Park Avenue Station'),
    ('Broadway Station'),
    ('Downtown Station'),
    ('North Gate Station');

INSERT INTO routes (name) VALUES
    ('Route A - Downtown'),
    ('Route B - North Side'),
    ('Route C - East District'),
    ('Route D - West End'),
    ('Route E - South Gate');

INSERT INTO routestations (routeid, stationid, stoporder, pickuptime) VALUES
    (1, 1, 1, '07:15 AM'),
    (1, 2, 2, '07:25 AM'),
    (1, 3, 3, '07:35 AM');

INSERT INTO routestations (routeid, stationid, stoporder, pickuptime) VALUES
    (2, 4, 1, '07:10 AM'),
    (2, 5, 2, '07:20 AM');

INSERT INTO routestations (routeid, stationid, stoporder, pickuptime) VALUES
    (3, 1, 1, '07:30 AM'),
    (3, 3, 2, '07:45 AM'),
    (4, 2, 1, '07:00 AM'),
    (4, 4, 2, '07:15 AM'),
    (5, 5, 1, '07:05 AM'),
    (5, 1, 2, '07:20 AM');

SELECT 'users'                AS TableName, COUNT(*) AS Rows FROM users
UNION ALL
SELECT 'subscriptions',                     COUNT(*) FROM subscriptions
UNION ALL
SELECT 'stations',                          COUNT(*) FROM stations
UNION ALL
SELECT 'routes',                            COUNT(*) FROM routes
UNION ALL
SELECT 'stationchangerequests',             COUNT(*) FROM stationchangerequests
UNION ALL
SELECT 'routechangerequests',               COUNT(*) FROM routechangerequests;

SELECT
    r.name        AS Route,
    rs.stoporder,
    s.name        AS Station,
    rs.pickuptime
FROM routestations rs
JOIN routes   r ON r.id = rs.routeid
JOIN stations s ON s.id = rs.stationid
ORDER BY r.id, rs.stoporder;