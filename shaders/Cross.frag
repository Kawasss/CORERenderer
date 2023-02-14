#version 460 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D Texture;
uniform int isSelected;

void main()
{
	vec4 color = texture(Texture, TexCoords).rgba;

	if (isSelected == 1 && color.r > 0.14 && color.r < 0.16)
		color = vec4(1, 0, 0, 1);

	FragColor = color;
}