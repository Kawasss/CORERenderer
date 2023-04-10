#version 430 core
layout (location = 0) in vec2 aPos;

uniform mat4 projection;
uniform mat4 model;

void main()
{
	vec3 Pos = (vec4(aPos, 0, 1) * model).xyz;
	gl_Position = projection * vec4(Pos, 1);
}