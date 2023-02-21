#version 460 core
out vec4 FragColor;

struct DirLight 
{
	vec3 direction;
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
};
uniform DirLight dirLight;

struct SpotLight
{
	vec3 position;
	vec3 direction;
	float cutOff;
	float outerCutOff;

	vec3 ambient;
	vec3 diffuse;
	vec3 specular;

	float constant;
	float linear;
	float quadratic;
};
uniform SpotLight spotLight;

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
#define NR_POINTS_LIGHTS 2
uniform PointLight pointLights[NR_POINTS_LIGHTS];

struct Material 
{
	sampler2D diffuse;
	sampler2D specular;
	float shininess;
};
uniform Material material;

uniform vec3 viewPos;
uniform float distanceObject;
uniform float transparency;
uniform int allowAlpha;

in vec2 TexCoords;

in vec3 Normal;
in vec3 FragPos;

vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir)
{
	vec3 lightDir = normalize(-light.direction);

	//calculating diffuse shading
	float diff = max(dot(normal, lightDir), 0);

	//calculating specular shading

	//Phong shading model
	//vec3 reflectDir = reflect(lightDir, normal); //if not working correctly try -lightDir
	//float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);

	//blinn-phong shading model
	vec3 halfwayDir = normalize(lightDir + viewDir);
	float spec = pow(max(dot(normal, halfwayDir), 0), material.shininess); //maybe material.shininess * 2??

	//putting everything together
	vec3 ambient = light.ambient * texture(material.diffuse, TexCoords).rgb;
	vec3 diffuse = light.diffuse * diff * pow(texture(material.diffuse, TexCoords).rgb, vec3(2.2));//texture(material.diffuse, TexCoords).rgb;
	vec3 specular = light.specular * spec * texture(material.specular, TexCoords).rgb;

	return (ambient + diffuse + specular);
}

vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir)
{
	vec3 lightDir = normalize(light.position - fragPos);
	
	//calculating diffuse shading
	float diff = max(dot(normal, lightDir), 0.0);
	
	//calculating specular shading
	
	//Phong shading model
	//vec3 reflectDir = reflect(lightDir, normal); //if not working correctly try -lightDir
	//float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);

	//blinn-phong shading model
	vec3 halfwayDir = normalize(lightDir + viewDir);
	float spec = pow(max(dot(normal, halfwayDir), 0), material.shininess); //maybe material.shininess * 2??
	
	//attenuation
	float Distance = length(light.position - fragPos);
	float attenuation = 1.0 / Distance;//(light.constant + light.linear * Distance + light.quadratic * Distance);//(Distance * Distance)
	
	//putting everything together
	vec3 ambient = light.ambient * texture(material.diffuse, TexCoords).rgb;
	vec3 diffuse = light.diffuse * diff * pow(texture(material.diffuse, TexCoords).rgb, vec3(2.2));//texture(material.diffuse, TexCoords).rgb;
	vec3 specular = light.specular * spec * texture(material.specular, TexCoords).rgb;
	
	ambient *= attenuation;
	diffuse *= attenuation;
	specular *= attenuation;
	
	return (ambient + diffuse + specular);
}

vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir)
{
	vec3 lightDir = normalize(light.position - fragPos);

	//calculating diffuse shading
	float diff = max(dot(normal, lightDir), 0.0);

	//calculating specular shading
	
	//Phong shading model
	//vec3 reflectDir = reflect(lightDir, normal); //if not working correctly try -lightDir
	//float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);

	//blinn-phong shading model
	vec3 halfwayDir = normalize(lightDir + viewDir);
	float spec = pow(max(dot(normal, halfwayDir), 0), material.shininess); //maybe material.shininess * 2??

	//attenuation
	float Distance = length(light.position - FragPos);
	float attenuation = 1 / Distance;//(light.constant + light.linear * Distance + light.quadratic * Distance);//(Distance * Distance)

	//calculating the intensity of the spotlight
	float theta = dot(lightDir, normalize(light.direction));
	float epsilon = light.cutOff - light.outerCutOff;
	float intensity = clamp((theta - light.outerCutOff) / epsilon, 0, 1);

	//putting everything together
	vec3 ambient = light.ambient * texture(material.diffuse, TexCoords).rgb;
	vec3 diffuse = light.diffuse * diff * pow(texture(material.diffuse, TexCoords).rgb, vec3(2.2));//texture(material.diffuse, TexCoords).rgb;
	vec3 specular = light.specular * spec * texture(material.specular, TexCoords).rgb;

	ambient *= attenuation * intensity;
	diffuse *= attenuation * intensity;
	specular *= attenuation * intensity;

	return (ambient + diffuse + specular);
}

float BlinnPhong(vec3 normal, vec3 fragPos, vec3 lightPos, vec3 lightColor)
{
    vec3 L = lightPos - fragPos;
	vec3 V = normalize(-fragPos);

	vec3 H = normalize(L + V);
	float result = max(dot(H, normal), 0.0);

	return result;
}

void main()
{
	/*vec3 color = texture(material.diffuse, TexCoords).rgb;
    // ambient
    vec3 ambient = 0.05 * color;
    // diffuse
    vec3 lightDir = normalize(pointLights[0].position - FragPos);
    vec3 normal = normalize(Normal);
    float diff = max(dot(lightDir, normal), 0.0);
    vec3 diffuse = diff * color;
    // specular
    vec3 viewDir = normalize(viewPos - FragPos);
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = 0.0;
        vec3 halfwayDir = normalize(lightDir + viewDir);  
        spec = pow(max(dot(normal, halfwayDir), 0.0), 32.0);
    vec3 specular = vec3(0.3) * spec; // assuming bright white light color
    FragColor = vec4(ambient + diffuse + specular, 1.0);*/
	vec4 color = texture(material.diffuse, TexCoords);
	if (allowAlpha == 1)
		color.a = transparency;

	if (color.a < 0.1)
		discard;

	FragColor = color;
}