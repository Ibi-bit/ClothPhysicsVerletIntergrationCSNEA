

-- Insert roles
INSERT INTO Roles (name) VALUES 
    ('teacher'),
    ('student');

-- Insert users (2 teachers, 5 students)
INSERT INTO Users (username, pass, role_id) VALUES
    ('mr_physics', 'password', 1),           -- Teacher 1
    ('ms_engineering', 'password', 1),       -- Teacher 2
    ('alice_student', 'password', 2),        -- Student 1
    ('bob_builder', 'password', 2),          -- Student 2
    ('charlie_mesh', 'password', 2),         -- Student 3
    ('diana_verlet', 'password', 2),         -- Student 4
    ('evan_springs', 'password',2);         -- Student 5

-- Insert assignments
INSERT INTO Assignments (title, description, teacher_id, due_date) VALUES
    ('Basic Bridge', 'Create a simple bridge structure using particles and sticks that can support a load.', 1, '2026-01-20 23:59:00'),
    ('Suspension Bridge', 'Design a suspension bridge with proper cable tension distribution.', 1, '2026-02-01 23:59:00'),
    ('Cloth Simulation', 'Create a realistic cloth simulation with at least 100 particles.', 2, '2026-01-25 23:59:00'),
    ('Truss Structure', 'Build a truss structure that maximizes strength-to-weight ratio.', 2, '2026-02-10 23:59:00'),
    ('Make a Wave', 'Create a wave simulation using particles and springs.', 2, '2026-02-15 23:59:00');

