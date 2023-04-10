#version 430 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D screenTexture;

uniform int useVignette;
uniform float vignetteStrength;

uniform int useChromaticAberration;
uniform vec3 chromAberIntensities;

void main()
{
	float distanceFromCentreOfScreen = length(TexCoords - 0.5);

	vec4 color;
	if (useChromaticAberration == 1)
	{
		vec2 rUV = TexCoords + chromAberIntensities.x * (TexCoords - 0.5);
		vec2 gUV = TexCoords + chromAberIntensities.y * (TexCoords - 0.5);
		vec2 bUV = TexCoords + chromAberIntensities.z * (TexCoords - 0.5);

		color.r = texture(screenTexture, rUV).r;
		color.g = texture(screenTexture, gUV).g;
		color.b = texture(screenTexture, bUV).b;
	}
	else
		color = texture(screenTexture, TexCoords);
	
	float vignetteColor = 0;
	if (useVignette == 1)
		vignetteColor = (distanceFromCentreOfScreen) * vignetteStrength;

	color -= vignetteColor;
	color.a = 1;

	FragColor = color;
}