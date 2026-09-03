CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO
-- 1. USERS TABLE
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Role NVARCHAR(20) NOT NULL CHECK (Role IN ('Organiser', 'Participant')),
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- 2. ORGANISERS TABLE
CREATE TABLE Organisers (
    OrganiserID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT UNIQUE NOT NULL,
    OrganisationName NVARCHAR(100) NOT NULL,
    ContactNumber NVARCHAR(20) NOT NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);
GO
-- 3. PARTICIPANTS TABLE
CREATE TABLE Participants (
    ParticipantID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT UNIQUE NOT NULL,
    DateOfBirth DATE NOT NULL,
    ProfilePictureURL NVARCHAR(500) NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);
GO

-- 4. EVENTS TABLE
CREATE TABLE Events (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    EventDate DATETIME NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    Distance DECIMAL(5,2) NOT NULL,
    EventType NVARCHAR(20) NOT NULL CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    BannerImageURL NVARCHAR(500) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (OrganiserID) REFERENCES Organisers(OrganiserID)
);
GO

-- 5. CATEGORIES TABLE
CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(200) NULL,
    MinAge INT NULL,
    MaxAge INT NULL,
    FOREIGN KEY (EventID) REFERENCES Events(EventID) ON DELETE CASCADE
);
GO

-- 6. ENROLMENTS TABLE
CREATE TABLE Enrolments (
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    EventID INT NOT NULL,
    CategoryID INT NOT NULL,
    RegistrationDate DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(20) DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled')),
    CONSTRAINT UQ_Enrolment UNIQUE (ParticipantID, EventID),
    FOREIGN KEY (ParticipantID) REFERENCES Participants(ParticipantID),
    FOREIGN KEY (EventID) REFERENCES Events(EventID),
    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
);
GO

-- 7. RESULTS TABLE
CREATE TABLE Results (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT UNIQUE NOT NULL,
    FinishTime TIME NOT NULL,
    FinishPosition INT NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Published' CHECK (Status IN ('Published', 'Pending', 'Disqualified')),
    FOREIGN KEY (EnrolmentID) REFERENCES Enrolments(EnrolmentID)
);
GO


-- Users
INSERT INTO Users (Email, PasswordHash, FullName, Role) VALUES
('race@pretoriasports.co.za', 'hash_pretoria_789', 'Pieter van der Merwe', 'Organiser'),
('events@durbanactive.com', 'hash_durban_456', 'Nthabiseng Mokoena', 'Organiser'),
('sipho.k@email.com', 'hash_sipho_123', 'Sipho Khumalo', 'Participant'),
('amanda.w@email.com', 'hash_amanda_456', 'Amanda Williams', 'Participant'),
('james.p@email.com', 'hash_james_789', 'James Patterson', 'Participant'),
('refiloe.m@email.com', 'hash_refiloe_101', 'Refiloe Mabaso', 'Participant'),
('lindiwe.n@email.com', 'hash_lindiwe_202', 'Lindiwe Ndlovu', 'Participant');

-- Organisers
INSERT INTO Organisers (UserID, OrganisationName, ContactNumber) VALUES
(1, 'Pretoria Sports Events', '+27 82 567 8901'),
(2, 'Durban Active Living', '+27 83 654 3210');

-- Participants
INSERT INTO Participants (UserID, DateOfBirth, ProfilePictureURL) VALUES
(3, '1994-08-10', NULL),
(4, '1991-12-05', NULL),
(5, '1987-03-19', NULL),
(6, '1993-06-25', NULL),
(7, '1998-11-30', NULL);

-- Events
INSERT INTO Events (OrganiserID, Name, Description, EventDate, Location, Distance, EventType) VALUES
(1, 'Pretoria Marathon', 'Flag to flag marathon through the administrative capital', '2026-10-25 06:00:00', 'Pretoria, Gauteng', 42.20, 'Run'),
(1, 'Voortrekker 5km Fun Run', 'Family friendly run in the Voortrekker Monument area', '2026-11-15 08:00:00', 'Pretoria, Gauteng', 5.00, 'Run'),
(2, 'Durban Beachfront Walk', 'Coastal walk along the golden mile', '2026-09-20 07:30:00', 'Durban, KwaZulu-Natal', 10.00, 'Walk'),
(2, 'Sugar Rush Cycling', 'Cycle through the sugar cane fields of KZN', '2026-10-05 06:30:00', 'Durban, KwaZulu-Natal', 65.00, 'Cycle');

-- Categories
INSERT INTO Categories (EventID, Name, Description, MinAge, MaxAge) VALUES
(1, 'Sub-Junior', 'Runners 14-17 years', 14, 17),
(1, 'Junior', 'Runners 18-22 years', 18, 22),
(1, 'Senior', 'Runners 23-39 years', 23, 39),
(1, 'Veteran', 'Runners 40-49 years', 40, 49),
(1, 'Masters', 'Runners 50+ years', 50, NULL),
(2, 'Kids', 'Children 8-12 years', 8, 12),
(2, 'Teens', 'Youth 13-17 years', 13, 17),
(2, 'Adults', 'Adults 18+ years', 18, NULL),
(3, 'Under 16', 'Walkers under 16', 10, 15),
(3, 'Open', 'Walkers 16-45 years', 16, 45),
(3, 'Golden', 'Walkers 46+ years', 46, NULL),
(4, 'Junior', 'Cyclists 16-19 years', 16, 19),
(4, 'Elite', 'Cyclists 20-34 years', 20, 34),
(4, 'Masters', 'Cyclists 35-49 years', 35, 49),
(4, 'Grand Masters', 'Cyclists 50+ years', 50, NULL);

-- Enrolments
INSERT INTO Enrolments (ParticipantID, EventID, CategoryID, Status) VALUES
(1, 1, 3, 'Confirmed'),
(1, 2, 8, 'Confirmed'),
(2, 4, 13, 'Pending'),
(2, 1, 4, 'Confirmed'),
(3, 3, 10, 'Confirmed'),
(3, 4, 14, 'Confirmed'),
(4, 1, 3, 'Pending'),
(4, 3, 11, 'Confirmed'),
(5, 2, 7, 'Confirmed'),
(5, 4, 13, 'Confirmed');

-- Results
INSERT INTO Results (EnrolmentID, FinishTime, FinishPosition) VALUES
(1, '03:15:30', 89),
(2, '00:28:45', 34),
(4, '04:45:20', 312),
(5, '01:15:50', 56),
(6, '02:55:10', 78),
(8, '01:30:15', 45),
(9, '00:32:20', 67),
(10, '03:25:40', 92);
GO
