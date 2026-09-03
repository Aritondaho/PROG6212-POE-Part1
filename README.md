# PROG6212-POE-Part1

## RaceDay System - Part 1 System Planning and Database

### System Description

RaceDay is a race event management system designed to manage organisers, participants, events, categories, event enrolments and race results.

Part 1 focuses on planning the database structure, RESTful API endpoints and SQL Server database schema before application development begins.

### User Roles

#### Organiser
Organisers can manage race events and categories, view participant enrolments and manage race results.

#### Participant

Participants can view available events and categories, enrol for events and view their race results and profile information.

### Planning Documents

The planning documents are available in the `docs` folder:

- [Entity Relationship Diagram](docs/Entity%20Relationship%20Diagram.jpg)
- [API and Database Plan](docs/API%20and%20Database%20Plan.pdf)
- [Database SQL Script](docs/Database%20SQL%20Script.sql)

### Database

The SQL script creates and populates the RaceDay database using SQL Server. It includes the required tables, primary keys, foreign keys, constraints and sample data.

### CI/CD

A GitHub Actions workflow is used to validate the repository structure and confirm that the required planning documents are present.
<img width="1267" height="622" alt="Screenshot 2026-09-03 234650" src="https://github.com/user-attachments/assets/d854aae1-e4f3-4666-b0cf-e095bf6368af" />



### YouTube Presentation

The unlisted YouTube presentation explains the planning documents, ERD decisions, API endpoint choices and the SQL script running in SQL Server Management Studio.

**YouTube Video:**  
_Add your unlisted YouTube video link here._

### Repository Structure

```text
PROG6212-POE-Part1/
│
├── docs/
│   ├── Entity Relationship Diagram.jpg
│   ├── API and Database Plan.pdf
│   ├── Database SQL Script.sql
│   └── .gitkeep
│
├── .github/
│   └── workflows/
│       └── validate.yml
│
└── README.md
