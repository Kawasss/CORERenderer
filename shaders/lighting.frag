#version 460 core
out vec4 FragColor;

struct PointLight
{
	vec3 position;
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;

	float constant;
	float linear;
	float quadratic;
};
#define NR_POINTS_LIGHTS 1
uniform PointLight pointLights[NR_POINTS_LIGHTS];

struct Material 
{
	sampler2D Texture;
	sampler2D diffuse;
	sampler2D specular;
	sampler2D normalMap;
	float shininess;
};
uniform Material material;

//uniform vec3 viewPos;
//uniform float distanceObject;
//uniform float transparency;
//uniform int allowAlpha;
//uniform vec3 overrideColor;
//uniform int hasNormalMap;
in vec3 overrideColor;
in vec3 Normal;
//in vec2 TexCoords;

//in vec3 Normal;
//in vec3 FragPos;

/*vec3 getNormalFromMap()
{
    vec3 tangentNormal = texture(material.normalMap, TexCoords).xyz * 2.0 - 1.0;

    vec3 Q1  = dFdx(FragPos);
    vec3 Q2  = dFdy(FragPos);
    vec2 st1 = dFdx(TexCoords);
    vec2 st2 = dFdy(TexCoords);

    vec3 N   = normalize(Normal);
    vec3 T  = normalize(Q1*st2.t - Q2*st1.t);
    vec3 B  = -normalize(cross(N, T));
    mat3 TBN = transpose(mat3(T, B, N));

    return normalize(TBN * tangentNormal);
}*/

void main()
{
	//if (overrideColor != vec3(0))
		FragColor = vec4(overrideColor, 1);
	//else
	//	FragColor = vec4(Normal, 1);
}