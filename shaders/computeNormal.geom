#version 430 core
layout(triangles) in;
layout(triangle_strip, max_vertices = 3) out;

in VS_OUT
{
	vec3 position;
	vec3 normal;
	vec3 fragPos;
	vec2 texCoords;
} gs_in[];

out vec3 FragPos;
out vec2 TexCoords;

out vec3 Normal;

void main()
{
	FragPos = gs_in[0].fragPos;
	TexCoords = gs_in[0].texCoords;

	vec3 p = cross(gs_in[1].position - gs_in[0].position, gs_in[2].position - gs_in[1].position);
	vec3 Normal = p;

	gl_Position = gl_in[0].gl_Position;
	EmitVertex();
	gl_Position = gl_in[1].gl_Position;
	FragPos = gs_in[1].fragPos;
	TexCoords = gs_in[1].texCoords;
	EmitVertex();
	gl_Position = gl_in[2].gl_Position;
	FragPos = gs_in[2].fragPos;
	TexCoords = gs_in[2].texCoords;
	EmitVertex();
	EndPrimitive();
}