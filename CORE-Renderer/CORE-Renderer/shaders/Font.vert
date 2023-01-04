#version 460 core
layout (location = 0) in vec4 vertex;

layout (std140, binding = 0) uniform Matrices
{
	mat4 projection;
	mat4 view;
};

out vec2 TexCoords;

uniform mat4 model;

void main()
{
    TexCoords = vertex.zw;
    gl_Position =  vec4(vertex.xy, 0, 1);//* model * view //projection * 
}