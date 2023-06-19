#version 430 core
out vec4 FragColor;
in vec3 Pos;

uniform samplerCube environmentMap;

const float PI = 3.14159265359;

void main()
{		
    FragColor = vec4(vec3(0.2), 1.0);
}