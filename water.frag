#version 430 core
out vec4 FragColor;

const float PI = 3.14159265359;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoords;

uniform float time;

uniform sampler2D normal1;
uniform sampler2D normal2;

uniform samplerCube reflection;

uniform vec3 lightPos[2];
uniform vec3 viewPos;
uniform vec3 absorbance;

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

    vec3 color = vec3(0);
    vec3 absorb = vec3(1) - absorbance;
    vec3 F0 = mix(texture(reflection, refract(-viewDir, normal, 1.33)).rgb, absorb, .2);
    /*for (int i = 0; i < 2; i++)
    {
        vec3 lightDir  = normalize(lightPos[i] - FragPos);
        vec3 H = normalize(viewDir + lightDir);

        float NDF = DistributionGGX(normal, H, 0);   
        float G   = GeometrySmith(normal, viewDir, lightDir, 0);      
        vec3 F    = fresnelSchlick(max(dot(H, viewDir), 0.0), absorb);

        vec3 numerator    = NDF * G * F; 
        float denominator = 4.0 * max(dot(normal, viewDir), 0.0) * max(dot(normal, lightDir), 0.0) + 0.0001; // + 0.0001 to prevent divide by zero
        vec3 specular = numerator / denominator;

        vec3 kS = F;
                    
        vec3 kD = vec3(1.0) - kS;

        // scale light by NdotL
        float NdotL = max(dot(normal, lightDir), 0.0);        

        // add to outgoing radiance Lo
        color += ((texture(reflection, refract(-viewDir, normal, 1.33)).rgb * absorb) / PI + specular) * NdotL;  // note that we already multiplied the BRDF by the Fresnel (kS) so we won't multiply by kS again
    }*/
    vec3 lightDir  = normalize(FragPos + vec3(0, 1, 0) - FragPos);
        vec3 H = normalize(viewDir + lightDir);

        float NDF = DistributionGGX(normal, H, 0);   
        float G   = GeometrySmith(normal, viewDir, lightDir, 0.5);      
        vec3 F    = fresnelSchlick(max(dot(H, viewDir), 0.0), F0);

        vec3 numerator    = NDF * G * F; 
        float denominator = 4.0 * max(dot(normal, viewDir), 0.0) * max(dot(normal, lightDir), 0.0) + 0.0001; // + 0.0001 to prevent divide by zero
        vec3 specular = numerator / denominator;

        vec3 kS = F;
                    
        vec3 kD = vec3(1.0) - kS;

        // scale light by NdotL
        float NdotL = max(dot(normal, lightDir), 0.0);        

        float fresnel = dot(normal, viewPos);
        fresnel = clamp(1 - fresnel, 0.0, 1.0);
        fresnel = pow(fresnel, 5);

        // add to outgoing radiance Lo
        color += (texture(reflection, refract(-viewDir, normal, 1.33)).rgb + specular) * NdotL;  // note that we already multiplied the BRDF by the Fresnel (kS) so we won't multiply by kS again
    // HDR tonemapping
    color /= (color + vec3(1.0));
    // gamma correct
    color = pow(color, vec3(1.0/2.2)); 
    color = texture(reflection, reflect(viewDir, normal)).rgb * fresnel + texture(reflection, refract(-viewDir, normal, 1.33)).rgb;
    FragColor = vec4(mix(color, absorb, .1)/*mix(texture(reflection, reflect(viewDir, normal)).rgb, texture(reflection, refract(viewDir, normal, 1.33)).rgb, fresnel)*/, 1.0);//
}