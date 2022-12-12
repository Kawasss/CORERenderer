#version 460 core
out vec4 FragColor;

in vec3 textureDir;
uniform samplerCube cubemap;

void main()
{
	FragColor = texture(cubemap, textureDir);
}