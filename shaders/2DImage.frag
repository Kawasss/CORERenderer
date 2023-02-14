#version 460 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D Texture;

void main()
{
	//vec3 color = pow(texture(Texture, TexCoords).rgb, vec3(1.0/2.2));
	//vec3 color = pow(texture(Texture, TexCoords).rgb, vec3(2.2));
	//vec3 color = texture(Texture, TexCoords).rgb;
	//FragColor = vec4(color, 1);

	vec3 envColor = texture(Texture, TexCoords).rgb;
    
    // HDR tonemap and gamma correct
    envColor = envColor / (envColor + vec3(1.0));
    envColor = pow(envColor, vec3(1.0/2.2)); 
    
    FragColor = vec4(envColor, 1.0);
}