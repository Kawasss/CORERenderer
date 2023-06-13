#version 430 core
out vec4 FragColor;

const float PI = 3.14159265359;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoords;

uniform float time;
uniform float farPlane;

uniform sampler2D normal1;
uniform sampler2D normal2;

uniform samplerCube reflection;
uniform samplerCube depthMap;

uniform vec3 lightPos;
uniform vec3 viewPos;
uniform vec3 absorbance;

float GetShadow(vec3 FragPos)
            {
                vec3 fragToLight = FragPos - lightPos[0];
                float currentDepth = length(fragToLight);

                float shadow = 0.0;
                float bias = 0.05;
                float samples = 3.0;
                float offset = 0.1;
                for(float x = -offset; x < offset; x += offset / (samples * 0.5))
                {
                    for(float y = -offset; y < offset; y += offset / (samples * 0.5))
                    {
                        for(float z = -offset; z < offset; z += offset / (samples * 0.5))
                        {
                            float closestDepth = texture(depthMap, fragToLight + vec3(x, y, z)).r;
                            closestDepth *= farPlane; // undo mapping [0;1]
                            if(currentDepth - bias > closestDepth)
                                shadow += 1.0;
                        }
                    }
                }
                shadow /= (samples * samples * samples);
                return 1 - shadow;
            }

float GetDistanceInWater(vec3 texCoords)
{
    return texture(depthMap, texCoords).r * farPlane;
}

vec3 getNormalFromMap(sampler2D normalMap, vec2 texCoords)
{
    vec3 tangentNormal = texture(normalMap, texCoords).xyz * 2.0 - 1.0;

    vec3 Q1  = dFdx(FragPos);
    vec3 Q2  = dFdy(FragPos);
    vec2 st1 = dFdx(texCoords);
    vec2 st2 = dFdy(texCoords);

    vec3 N   = normalize(Normal);
    vec3 T  = normalize(Q1*st2.t - Q2*st1.t);
    vec3 B  = -normalize(cross(N, T));
    mat3 TBN = mat3(T, B, N);

    return normalize(TBN * tangentNormal);
}

vec3 GetGuassianBlur(vec3 viewDir, vec3 normal)
{
    vec3 dir = reflect(-viewDir, normal);
    vec3 highestReflection = texture(reflection, dir).rgb;
    return highestReflection;
}

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness*roughness;
    float a2 = a*a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH*NdotH;

    float nom   = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;

    return nom / denom;
}
            
float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float nom   = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return nom / denom;
}
            
float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}
            
vec3 fresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}
            
void main()
{
	vec2 texCoords = vec2(TexCoords.x + time * 0.01, TexCoords.y);
    vec2 texCoords2 = vec2(TexCoords.x, TexCoords.y + time * 0.01);
    vec3 normal = normalize((getNormalFromMap(normal1, texCoords) + getNormalFromMap(normal2, texCoords2)));
    vec3 viewDir = normalize(viewPos - FragPos);
    vec3 directionThroughWater = reflect(-(viewPos - FragPos), normal);

    vec3 color = vec3(0);
    vec3 absorb = vec3(0, 0.53, 1);
    vec3 locationOfClosestObject = FragPos + directionThroughWater * GetDistanceInWater(directionThroughWater);
    float mixingValue = exp(-distance(FragPos, locationOfClosestObject));
    vec3 F0 = mix(texture(reflection, directionThroughWater).rgb, absorb, mixingValue);
    vec3 lightDir  = normalize(lightPos - FragPos);

    vec3 ambient = texture(reflection, directionThroughWater).rgb * 0.1;

    vec3 result = F0;
    //color = texture(reflection, reflect(viewDir, normal)).rgb * fresnel + texture(reflection, refract(-viewDir, normal, 1.33)).rgb;
    FragColor = vec4(ambient + texture(reflection, directionThroughWater).rgb * fresnelSchlick(dot(-(viewPos - FragPos), normal), F0) * GetShadow(FragPos)/*mix(texture(reflection, reflect(viewDir, normal)).rgb, texture(reflection, refract(viewDir, normal, 1.33)).rgb, fresnel)*/, 1.0);//
}