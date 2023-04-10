#version 430 core
out vec4 FragColor;
in vec3 TexCoords;

uniform samplerCube environmentMap;

void main()
{		
    vec3 envColor = texture(environmentMap, TexCoords).rgb;
    
    // HDR tonemap and gamma correct (not needed because it isnt srgb color format
    //envColor = envColor / (envColor + vec3(1.0));
    //envColor = pow(envColor, vec3(1.0/2.2)); 
    
    FragColor = vec4(envColor, 1.0);
}