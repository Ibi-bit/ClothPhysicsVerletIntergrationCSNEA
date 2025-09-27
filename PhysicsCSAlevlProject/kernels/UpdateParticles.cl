// OpenCL kernel for updating particle positions using Verlet integration
// Each work item processes one particle

typedef struct {
    float2 position;        // Current position
    float2 previousPosition; // Previous position 
    float2 accumulatedForce; // Accumulated forces
    float mass;              // Particle mass
    int isPinned;            // Whether particle is pinned (0 or 1)
} Particle;

typedef struct {
    int2 draggedParticleIndices[1024]; // Max 1024 dragged particles
    int draggedCount;                  // Number of dragged particles
    float2 mousePosition;              // Current mouse position
    float2 gravity;                    // Gravity force
    float2 windForce;                  // Wind force
    float deltaTime;                   // Time step
    float drag;                        // Drag coefficient
    float screenWidth;                 // Screen bounds
    float screenHeight;
} PhysicsParams;

__kernel void updateParticles(
    __global Particle* particles,
    __global const PhysicsParams* params,
    const int width,
    const int height)
{
    int idx = get_global_id(0);
    int particleCount = width * height;
    
    if (idx >= particleCount) return;
    
    // Convert linear index to 2D coordinates
    int i = idx / height;
    int j = idx % height;
    
    Particle p = particles[idx];
    
    // Skip pinned particles
    if (p.isPinned) return;
    
    // Check if particle is being dragged
    bool isBeingDragged = false;
    for (int d = 0; d < params->draggedCount; d++) {
        if (params->draggedParticleIndices[d].x == i && 
            params->draggedParticleIndices[d].y == j) {
            isBeingDragged = true;
            break;
        }
    }
    
    if (!isBeingDragged) {
        // Calculate total force (gravity + accumulated forces + wind)
        float2 totalForce = params->gravity + p.accumulatedForce + params->windForce;
        float2 acceleration = totalForce / p.mass;
        
        // Verlet integration
        float2 velocity = p.position - p.previousPosition;
        velocity *= params->drag;
        
        float2 previousPosition = p.position;
        p.position = p.position + velocity + acceleration * (params->deltaTime * params->deltaTime);
        p.previousPosition = previousPosition;
        
        // Keep particle inside screen bounds
        if (p.position.x < 0) p.position.x = 0;
        if (p.position.x > params->screenWidth) p.position.x = params->screenWidth;
        if (p.position.y < 0) p.position.y = 0;
        if (p.position.y > params->screenHeight) p.position.y = params->screenHeight;
    }
    
    // Reset accumulated forces
    p.accumulatedForce = (float2)(0.0f, 0.0f);
    
    // Write back to global memory
    particles[idx] = p;
}