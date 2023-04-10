#version 430 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;

layout (std140, binding = 0) uniform Matrices
{
	mat4 projection;
	mat4 view;
};

out mat4 Model;

uniform mat4 model;
out vec2 TexCoords;
out vec3 FragPos;
out vec3 Normal;

void main() 
{
	FragPos = (vec4(aPos, 1.0) * model).xyz;
	Normal = mat3(transpose(inverse(model))) * aNormal; //way more efficient if calculated on CPU
	TexCoords = aTexCoords;
	Model = model;
	
	gl_Position = vec4(FragPos, 1) * view * projection;
}