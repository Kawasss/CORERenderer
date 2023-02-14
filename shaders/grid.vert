#version 460 core
layout (std140, binding = 0) uniform Matrices
{
	mat4 projection;
	mat4 view;
	mat4 translationlessView;
};

out vec3 coor;
out vec3 oPlayerPos;

out mat4 oModel;
out mat4 oView;
out mat4 oProjection;

uniform mat4 model;
//uniform mat4 view;
//uniform mat4 projection;
uniform vec3 playerPos;

vec3 coordinates[6] = vec3[](
	vec3(-1, 0, -1), vec3(1, 0, 1), vec3(1, 0, -1),
    vec3(1, 0, 1), vec3(-1, 0, -1), vec3(-1, 0, 1)
);

void main() 
{
	oModel = model;
	oView = view;
	oProjection = projection;
	oPlayerPos = playerPos;

	vec3 v1 = coordinates[gl_VertexID].xyz;
	coor = v1;
	gl_Position = vec4(v1, 1.0) * model * view * projection;
}