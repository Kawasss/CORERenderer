#version 460 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoords;

layout (std140, binding = 0) uniform Matrices
{
	mat4 projection;
	mat4 view;
};

out vec2 TexCoords;

uniform mat4 model;

void main()
{
	TexCoords = aTexCoords;
	gl_Position = vec4(aPos.x, 0, aPos.y, 1) * model * view * projection;
}