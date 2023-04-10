#version 430 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D Texture;
uniform vec3 textColor;

void main()
{
	vec4 text = vec4(1, 1, 1, texture(Texture, TexCoords).r);

	FragColor = vec4(textColor, 1) * text;
}