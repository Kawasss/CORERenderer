#version 460 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;

out VS_OUT
{
	vec3 normal;
} vs_out;

layout (std140, binding = 0) uniform Matrices
{
	mat4 projection;
	mat4 view;
};

uniform mat4 model;

void main()
{
	mat3 normalMatrix = mat3(transpose(inverse(view * model)));
	vs_out.normal = vec3(vec4(aNormal * normalMatrix, 0));
	gl_Position = vec4(aPos, 1) * model * view;
}