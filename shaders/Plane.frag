#version 460 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D Texture;

void main()
{
	//vec3 tex = texture(Texture, TexCoords).rgb;
	FragColor = texture(Texture, TexCoords);
}