#version 460 core

out vec3 TexCoords;

uniform mat4 projection;
uniform mat4 view;

vec3 coordinates[36] = vec3[](
	    vec3(-1.0f,  1.0f, -1.0f),
        vec3(-1.0f, -1.0f, -1.0f),
        vec3( 1.0f, -1.0f, -1.0f),
        vec3( 1.0f, -1.0f, -1.0f),
        vec3( 1.0f,  1.0f, -1.0f),
        vec3(-1.0f,  1.0f, -1.0f),

        vec3(-1.0f, -1.0f,  1.0f),
        vec3(-1.0f, -1.0f, -1.0f),
        vec3(-1.0f,  1.0f, -1.0f),
        vec3(-1.0f,  1.0f, -1.0f),
        vec3(-1.0f,  1.0f,  1.0f),
        vec3(-1.0f, -1.0f,  1.0f),

        vec3( 1.0f, -1.0f, -1.0f),
        vec3( 1.0f, -1.0f,  1.0f),
        vec3( 1.0f,  1.0f,  1.0f),
        vec3( 1.0f,  1.0f,  1.0f),
        vec3( 1.0f,  1.0f, -1.0f),
        vec3( 1.0f, -1.0f, -1.0f),

        vec3(-1.0f, -1.0f,  1.0f),
        vec3(-1.0f,  1.0f,  1.0f),
        vec3( 1.0f,  1.0f,  1.0f),
        vec3( 1.0f,  1.0f,  1.0f),
        vec3( 1.0f, -1.0f,  1.0f),
        vec3(-1.0f, -1.0f,  1.0f),

        vec3(-1.0f,  1.0f, -1.0f),
        vec3( 1.0f,  1.0f, -1.0f),
        vec3( 1.0f,  1.0f,  1.0f),
        vec3( 1.0f,  1.0f,  1.0f),
        vec3(-1.0f,  1.0f,  1.0f),
        vec3(-1.0f,  1.0f, -1.0f),

        vec3(-1.0f, -1.0f, -1.0f),
        vec3(-1.0f, -1.0f,  1.0f),
        vec3( 1.0f, -1.0f, -1.0f),
        vec3( 1.0f, -1.0f, -1.0f),
        vec3(-1.0f, -1.0f,  1.0f),
        vec3( 1.0f, -1.0f,  1.0f)
);

void main()
{
	TexCoords = coordinates[gl_VertexID];
    vec4 temp =  vec4(TexCoords, 1) * view * projection;
	gl_Position = temp.xyww;
}