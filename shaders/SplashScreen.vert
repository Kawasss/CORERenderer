#version 430 core
out vec2 TexCoords;

vec4[] vertices =
{
    vec4(-1,  1,  0,  1),
    vec4(-1, -1,  0,  0),
    vec4(1, -1,  1,  0),

    vec4(-1,  1,  0,  1),
    vec4(1, -1,  1,  0),
    vec4(1,  1,  1,  1)
};

void main()
{
    gl_Position = vec4(vertices[gl_VertexID].xy, 0, 1);
    TexCoords = vertices[gl_VertexID].zw;
}