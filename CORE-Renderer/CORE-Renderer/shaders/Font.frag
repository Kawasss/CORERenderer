#version 460 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D Texture;
uniform vec3 textColor;

void main()
{
	vec2 uv = TexCoords.xy;
	float text = texture(Texture, uv).r;
	
	FragColor = vec4(textColor.rgb * text, text);
}