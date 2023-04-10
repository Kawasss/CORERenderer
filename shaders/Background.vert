#version 430 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;

layout (std140, binding = 0) uniform Matrices
{
	mat4 projection;
	mat4 view;
	mat4 translationlessView;
};

out vec3 TexCoords;

void main()
{
    TexCoords = aPos;
	mat4 cView = mat4(mat3(view));
	vec4 clipPos = vec4(TexCoords, 1.0) * cView * projection;//translationlessView

	gl_Position = clipPos.xyww;//.xyww
}