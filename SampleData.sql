-- =============================================
-- Sample Data for Stick Simulation Database
-- Run this after creating the schema (StructuresDB.sql)
-- =============================================

-- Insert roles
INSERT INTO Roles (name) VALUES 
    ('teacher'),
    ('student');

-- Insert users (2 teachers, 5 students)
INSERT INTO Users (username, role_id) VALUES
    ('mr_physics', 1),           -- Teacher 1
    ('ms_engineering', 1),       -- Teacher 2
    ('alice_student', 2),        -- Student 1
    ('bob_builder', 2),          -- Student 2
    ('charlie_mesh', 2),         -- Student 3
    ('diana_verlet', 2),         -- Student 4
    ('evan_springs', 2);         -- Student 5

-- Insert assignments
INSERT INTO Assignments (title, description, teacher_id, due_date) VALUES
    ('Basic Bridge', 'Create a simple bridge structure using particles and sticks that can support a load.', 1, '2026-01-20 23:59:00'),
    ('Suspension Bridge', 'Design a suspension bridge with proper cable tension distribution.', 1, '2026-02-01 23:59:00'),
    ('Cloth Simulation', 'Create a realistic cloth simulation with at least 100 particles.', 2, '2026-01-25 23:59:00'),
    ('Truss Structure', 'Build a truss structure that maximizes strength-to-weight ratio.', 2, '2026-02-10 23:59:00');

-- Insert structures (student submissions with JSON content)
INSERT INTO Structures (assignment_id, student_id, content, submitted_at, grade, feedback) VALUES
    -- Alice's bridge submission
    (1, 3, '{
        "particles": [
            {"id": 0, "x": 100, "y": 300, "pinned": true},
            {"id": 1, "x": 200, "y": 300, "pinned": false},
            {"id": 2, "x": 300, "y": 300, "pinned": false},
            {"id": 3, "x": 400, "y": 300, "pinned": true},
            {"id": 4, "x": 150, "y": 250, "pinned": false},
            {"id": 5, "x": 250, "y": 250, "pinned": false},
            {"id": 6, "x": 350, "y": 250, "pinned": false}
        ],
        "sticks": [
            {"p1": 0, "p2": 1},
            {"p1": 1, "p2": 2},
            {"p1": 2, "p2": 3},
            {"p1": 0, "p2": 4},
            {"p1": 4, "p2": 5},
            {"p1": 5, "p2": 6},
            {"p1": 6, "p2": 3},
            {"p1": 4, "p2": 1},
            {"p1": 5, "p2": 2},
            {"p1": 6, "p2": 2}
        ],
        "springConstant": 50.0
    }', '2026-01-18 14:30:00', 85.50, 'Good triangular support structure. Consider adding more cross-bracing.'),

    -- Bob's bridge submission
    (1, 4, '{
        "particles": [
            {"id": 0, "x": 50, "y": 350, "pinned": true},
            {"id": 1, "x": 150, "y": 350, "pinned": false},
            {"id": 2, "x": 250, "y": 350, "pinned": false},
            {"id": 3, "x": 350, "y": 350, "pinned": false},
            {"id": 4, "x": 450, "y": 350, "pinned": true}
        ],
        "sticks": [
            {"p1": 0, "p2": 1},
            {"p1": 1, "p2": 2},
            {"p1": 2, "p2": 3},
            {"p1": 3, "p2": 4}
        ],
        "springConstant": 30.0
    }', '2026-01-19 09:15:00', 62.00, 'Basic structure but lacks triangulation. Bridge will sag under load.'),

    -- Charlie's cloth simulation
    (3, 5, '{
        "gridWidth": 10,
        "gridHeight": 10,
        "particles": 100,
        "pinnedTop": true,
        "springConstant": 45.0,
        "drag": 0.98
    }', '2026-01-24 16:45:00', 92.00, 'Excellent cloth behavior! Great use of drag coefficient.'),

    -- Diana's suspension bridge (not yet graded)
    (2, 6, '{
        "particles": [
            {"id": 0, "x": 0, "y": 200, "pinned": true},
            {"id": 1, "x": 100, "y": 100, "pinned": true},
            {"id": 2, "x": 200, "y": 50, "pinned": false},
            {"id": 3, "x": 300, "y": 50, "pinned": false},
            {"id": 4, "x": 400, "y": 100, "pinned": true},
            {"id": 5, "x": 500, "y": 200, "pinned": true}
        ],
        "cables": [
            {"p1": 0, "p2": 1},
            {"p1": 1, "p2": 2},
            {"p1": 2, "p2": 3},
            {"p1": 3, "p2": 4},
            {"p1": 4, "p2": 5}
        ],
        "springConstant": 60.0
    }', '2026-01-30 11:20:00', NULL, NULL),

    -- Evan's truss structure (not yet submitted - content is a work in progress)
    (4, 7, '{
        "status": "in_progress",
        "particles": [
            {"id": 0, "x": 100, "y": 400, "pinned": true},
            {"id": 1, "x": 200, "y": 400, "pinned": false}
        ],
        "sticks": []
    }', NULL, NULL, NULL);
