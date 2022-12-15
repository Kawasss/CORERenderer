#version 460 core

out vec2 texCoords;

vec3 coordinates[36] = vec3[](
        vec3(-1.0,  1.0, -1.0),
        vec3(-1.0, -1.0, -1.0),
        vec3( 1.0, -1.0, -1.0),
        vec3( 1.0, -1.0, -1.0),
        vec3( 1.0,  1.0, -1.0),
        vec3(-1.0,  1.0, -1.0),

        vec3(-1.0, -1.0,  1.0),
        vec3(-1.0, -1.0, -1.0),
        vec3(-1.0,  1.0, -1.0),
        vec3(-1.0,  1.0, -1.0),
        vec3(-1.0,  1.0,  1.0),
        vec3(-1.0, -1.0,  1.0),

        vec3( 1.0, -1.0, -1.0),
        vec3( 1.0, -1.0,  1.0),
        vec3( 1.0,  1.0,  1.0),
        vec3( 1.0,  1.0,  1.0),
        vec3( 1.0,  1.0, -1.0),
        vec3( 1.0, -1.0, -1.0),

        vec3(-1.0, -1.0,  1.0),
        vec3(-1.0,  1.0,  1.0),
        vec3( 1.0,  1.0,  1.0),
        vec3( 1.0,  1.0,  1.0),
        vec3( 1.0, -1.0,  1.0),
        vec3(-1.0, -1.0,  1.0),

        vec3(-1.0,  1.0, -1.0),
        vec3( 1.0,  1.0, -1.0),
        vec3( 1.0,  1.0,  1.0),
        vec3( 1.0,  1.0,  1.0),
        vec3(-1.0,  1.0,  1.0),
        vec3(-1.0,  1.0, -1.0),

        vec3(-1.0, -1.0, -1.0),
        vec3(-1.0, -1.0,  1.0),
        vec3( 1.0, -1.0, -1.0),
        vec3( 1.0, -1.0, -1.0),
        vec3(-1.0, -1.0,  1.0),
        vec3( 1.0, -1.0,  1.0)
);

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    texCoords = coordinates[gl_VertexID].xy;    
    gl_Position = projection * view * model * vec4(coordinates[gl_VertexID], 1.0);
}