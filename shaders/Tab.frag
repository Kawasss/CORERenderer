#version 460 core
out vec4 FragColor;

uniform int selected;
uniform int notImplemented;

void main()
{
	if (notImplemented == 1)
		FragColor = vec4(0.5, 0, 0, 1);
	else if (selected == 1)
		FragColor = vec4(0.15, 0.15, 0.15, 1);
	else if (selected == 0)
		FragColor = vec4(0.125, 0.125, 0.125, 1);
}