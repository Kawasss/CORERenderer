#version 460 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoords;

layout (std140, binding = 0) uniform Matrices
{
	mat4 projection;
	mat4 view;
};

float[] vertices =
{
    0.0, -1.0,   0.0, 0.0,
    0.0,  0.0,   0.0, 1.0,
    1.0,  0.0,   1.0, 1.0,
    0.0, -1.0,   0.0, 0.0,
    1.0,  0.0,   1.0, 1.0,
    1.0, -1.0,   1.0, 0.0
};

out vec2 TexCoords;

uniform mat4 model;

void main()
{
	//TexCoords = vec2(vertices[gl_VertexID * 4 + 2], vertices[gl_VertexID * 4 + 3]);
	//gl_Position = vec4(vertices[gl_VertexID * 4], vertices[gl_VertexID * 4 + 1], 0, 1) * model * projection;
    TexCoords = aTexCoords;
    gl_Position = vec4(aPos, 0, 1) * model * projection;
}