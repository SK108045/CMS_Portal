-- Run as the PostgreSQL administrator (psql -U postgres -f database/setup.sql).
-- psql prompts for a password, keeping it out of this repository.
CREATE ROLE mission_app LOGIN;
\password mission_app
CREATE DATABASE mission_school OWNER mission_app;
