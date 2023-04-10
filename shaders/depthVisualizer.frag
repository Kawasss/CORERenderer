#version 430 core
out vec4 FragColor;

//given camera values in camera.cs
float near = 0.001; 
float far = 10; //not 100, because youd need to be too far away for it to turn white

float LinearizeDepth(float depth)
{
	float NDC = depth * 2 - 1;
	return (2 * near * far) / (far + near - NDC * (far - near)); //linearDepth

}

void main()
{
	FragColor = vec4(vec3(LinearizeDepth(gl_FragCoord.z) / far), 1.0); //vec3(DEPTH)
}