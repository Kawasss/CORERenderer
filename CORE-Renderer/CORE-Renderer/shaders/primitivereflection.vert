#version 460 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec3 aTexCoords;

out vec3 Normal;
out vec3 Position;
uniform mat4 model;

uniform mat4 view;
uniform mat4 projection;

void main()
{
	Normal = mat3(transpose(inverse(model))) * aNormal;
	Position = vec3(vec4(aPos, 1.0) * model).xyz;
	gl_Position = vec4(Position, 1.0) * view * projection;
}