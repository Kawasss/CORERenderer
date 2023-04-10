#version 430 core
out vec4 FragColor;

uniform vec3 color;
uniform float alpha;

void main()
{
	float alpha2 = alpha;
	if (alpha == 0)
		alpha2 = 1;

	FragColor = vec4(color, alpha2);
}