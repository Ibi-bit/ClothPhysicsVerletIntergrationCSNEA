-- PostgreSQL Schema for Stick Simulation Database
-- Run the DROP/CREATE DATABASE commands separately (outside a transaction)
-- Then connect to stick_simulation and run the rest

DROP DATABASE IF EXISTS stick_simulation;
CREATE DATABASE stick_simulation;



CREATE TYPE role_type AS ENUM ('teacher', 'student');

CREATE TABLE Roles
(
    id SERIAL PRIMARY KEY,
    name role_type UNIQUE NOT NULL
);

CREATE TABLE Users
(
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    role_id INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (role_id) REFERENCES Roles(id)
);

CREATE TABLE Assignments
(
    id SERIAL PRIMARY KEY,
    title VARCHAR(100) NOT NULL,
    description TEXT,
    teacher_id INT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    due_date TIMESTAMP,
    FOREIGN KEY (teacher_id) REFERENCES Users(id)
);

CREATE TABLE Structures
(
    id SERIAL PRIMARY KEY,
    assignment_id INT,
    student_id INT NOT NULL,
    content JSONB,
    submitted_at TIMESTAMP,
    grade DECIMAL(5,2),
    feedback TEXT,
    FOREIGN KEY (assignment_id) REFERENCES Assignments(id) ON DELETE CASCADE,
    FOREIGN KEY (student_id) REFERENCES Users(id) ON DELETE CASCADE,
    UNIQUE (assignment_id, student_id)
);