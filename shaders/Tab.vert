#version 460 core
layout (location = 0) in vec2 aPos;

uniform mat4 projection;
uniform mat4 model;

void main()
{
	vec4 Pos = vec4(aPos, 0, 1) * model;

	gl_Position = projection * Pos;
}