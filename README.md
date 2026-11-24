# Secon-SBTi

A digital tool for collecting and visualizing sustainability data (SBTi) for Secon

# About The Project

SeconScope is a web application developed for Svensk Energi Consult AB (Secon). The tool streamlines the process of collecting, calculating, and visualizing greenhouse gas emissions data (Scope 1, 2, and 3) in accordance with the Science Based Targets initiative (SBTi) standards.

The goal of the project is to eliminate manual calculation bottlenecks, allowing Secon to autonomously manage their sustainability reporting, track monthly performance, and easily communicate their environmental impact to clients and internal stakeholders.

# Built With

Frontend: React, JavaScript

Backend: C#, .NET [Core / 10.0]

Database: 

# How to run the project

The project has three parts that run on your machine: a PostgreSQL database, a backend (.NET) and a frontend (React). Do the steps below in order.

> [!TIP]
> If something goes wrong, give an AI the error message together with this README and ask for help. The README tells it how the project is set up which makes it much more likely to find the problem.

### You need

- Git
- [PostgreSQL](https://www.postgresql.org/download/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/), LTS version (npm comes with it)
- A terminal. Git Bash or the built-in Terminal on macOS

## 1. Get the code

Clone this repo and switch to the `develop` branch.

## 2. Set up the PostgreSQL database

1. Install PostgreSQL. During the install you choose a password for the `postgres` user. You will need this password later.

2. Create a new database called `secon_sbti`. It's most convenient to use pgAdmin for this.

## 3. Run the database migrations

The tables are created by the SQL files in `infrastructure/migrations/`. Run them one at a time, in order of the version number (`V1__...`, then `V2__...` and so on). It's most convenient to use pgAdmin's Query Tool to run the migrations. When someone adds a new migration file later, run only that new file.

## 4. Start the backend

1. In `backend/`, make a copy of `appsettings.Development.json.example` and name it `appsettings.Development.json`.

2. Open the copy and replace `DB_NAME`, `USERNAME` and `PASSWORD` with your database name (`secon_sbti`), `postgres` and your password. The file is in `.gitignore` so your password is never committed.

3. Open a terminal in `backend/` and run:

   ```
   dotnet run
   ```

   If it worked, the backend runs on http://localhost:5101. The API docs are at http://localhost:5101/scalar.

If the backend can't reach the database, it stops right away and prints what is wrong, for example a wrong password or a missing database.

## 5. Start the frontend

1. Keep the backend running and open a new terminal in `frontend/`.

2. Run these commands in order:

   ```
   npm install
   npm run dev
   ```

3. Open the address shown in the terminal, usually http://localhost:5173.

The frontend gets its data from the backend, so the backend has to be running at the same time. `npm install` is only needed the first time and when `package.json` has changed.

## Starting again later

1. Make sure PostgreSQL is running.
2. Run `dotnet run` in `backend/`.
3. Run `npm run dev` in `frontend/`.

If new migration files have been added since last time, run them first (see step 3).
