#version 430 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoords;
layout (location = 2) in vec3 aNormal;
layout (location = 3) in ivec4 bonesID1;
layout (location = 4) in ivec4 bonesID2;
layout (location = 5) in vec4 weights1;
layout (location = 6) in vec4 weights2;

layout (std140, binding = 0) uniform Matrices
{
    mat4 projection;
    mat4 view;
};

out mat4 Model;

uniform mat4 model;
out vec3 Normal;
out vec2 TexCoords;
out vec3 FragPos;

void main() 
{
    vec4 pos = (vec4(aPos, 1) * model);

    FragPos = pos.xyz;
    Normal = aNormal * mat3(transpose(inverse(model))); //way more efficient if calculated on CPU
    Model = model;
    TexCoords = aTexCoords;

    gl_Position = pos * view * projection;
}