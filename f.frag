 #version 430 core
            out vec4 FragColor;

            #define PI 3.14159265359

            uniform vec3 viewPos;
            uniform vec3 lightPos;
            uniform float transparency;
            uniform int allowAlpha;
            uniform vec3 overrideColor;
            uniform int hasNormalMap;

            in vec2 TexCoords;

            in vec3 Normal;
            in vec3 FragPos;
            in mat4 Model;

            uniform sampler2D diffuseMap;
            uniform sampler2D specularMap;
            uniform sampler2D normalMap;
            uniform sampler2D metalMap;
            uniform sampler2D aoMap;
            uniform sampler2D displacementMap;

            uniform samplerCube skybox;

            #define heightScale 0.1

            vec3 getNormalFromMap()
            {
                vec3 normalMapColor = texture(normalMap, TexCoords).rgb;
                //normalMapColor.y = -normalMapColor.y;
                vec3 tangentNormal = normalMapColor * 2.0 - 1.0;

                vec3 Q1  = dFdx(FragPos);
                vec3 Q2  = dFdy(FragPos);
                vec2 st1 = dFdx(TexCoords);
                vec2 st2 = dFdy(TexCoords);

                vec3 N   = normalize(Normal);
                vec3 T  = normalize(Q1*st2.t + Q2*st1.t);
                vec3 B  = -normalize(cross(N, T));
                mat3 TBN = mat3(T, B, N);

                return normalize(TBN * tangentNormal);
            }
            vec2 ParallaxMapping(vec2 texCoords, vec3 viewDir)
            { 
                    // number of depth layers
            const float minLayers = 8;
            const float maxLayers = 32;
            float numLayers = mix(maxLayers, minLayers, abs(dot(vec3(0.0, 0.0, 1.0), viewDir)));  
            // calculate the size of each layer
            float layerDepth = 1.0 / numLayers;
            // depth of current layer
            float currentLayerDepth = 0.0;
            // the amount to shift the texture coordinates per layer (from vector P)
            vec2 P = viewDir.xy / viewDir.z * heightScale; 
            vec2 deltaTexCoords = P / numLayers;

            // get initial values
            vec2  currentTexCoords     = texCoords;
            float currentDepthMapValue = texture(displacementMap, currentTexCoords).r;

            while(currentLayerDepth < currentDepthMapValue)
            {
                // shift texture coordinates along direction of P
                currentTexCoords -= deltaTexCoords;
                // get depthmap value at current texture coordinates
                currentDepthMapValue = texture(displacementMap, currentTexCoords).r;  
                // get depth of next layer
                currentLayerDepth += layerDepth;  
            }

            // get texture coordinates before collision (reverse operations)
            vec2 prevTexCoords = currentTexCoords + deltaTexCoords;

            // get depth after and before collision for linear interpolation
            float afterDepth  = currentDepthMapValue - currentLayerDepth;
            float beforeDepth = texture(displacementMap, prevTexCoords).r - currentLayerDepth + layerDepth;

            // interpolation of texture coordinates
            float weight = afterDepth / (afterDepth - beforeDepth);
            vec2 finalTexCoords = prevTexCoords * weight + currentTexCoords * (1.0 - weight);

            return finalTexCoords;
            }
            void main()
            {
                vec3 viewDir = normalize((viewPos - FragPos));
                vec2 texCoords = ParallaxMapping(TexCoords, viewDir); 
                
                //if(texCoords.x > 1.0 || texCoords.y > 1.0 || texCoords.x < 0.0 || texCoords.y < 0.0)
                    //discard;

            	vec4 fullColor = texture(diffuseMap, texCoords);
            	if (fullColor.a < 0.1)
            		discard;
                    if (overrideColor != vec3(0))
                    {
                        vec3 lightDir = normalize((lightPos - FragPos));
                        vec3 normal = getNormalFromMap();
                        //vec3 viewDir = normalize((viewPos - FragPos));
                	    //vec3 reflectDir = reflect(-lightDir, normal);
                	    vec3 halfwayDir = normalize((lightDir + viewDir));
            	        float spec = pow(max(dot(normal, halfwayDir), 0), 225);//float spec = pow(max(dot(normal, halfwayDir), 0), 1);
                	    vec3 specular = vec3(.3) * spec * texture(specularMap, texCoords).rgb; // assuming bright white light color

                        FragColor = vec4(specular, transparency);
                        return;
                    }

            	vec3 color = pow(texture(diffuseMap, texCoords).rgb, vec3(2.2));
                float metallic = 1 - texture(metalMap, texCoords).r;

                vec3 ViewPos = (vec4(viewPos, 1)).xyz;
                vec3 normal = getNormalFromMap();

                //metalness (test)
                vec3 I = normalize(FragPos - viewPos);
                vec3 R = reflect(I, normalize(normal));//refract(I, normal, 1.00 / 1.52);
            
                vec3 reflection = texture(skybox, R).rgb;
                float strength = (reflection.r + reflection.g + reflection.b) / 3;
                vec3 reflectiveness = metallic * reflection * vec3(.3);

            	// ambient
                float ambientStrength = length(normalize(reflection));
                vec3 ambient = color * strength * texture(aoMap, texCoords).r + (reflection * metallic) * vec3(.1);

            	// diffuse
                float distance = distance(lightPos, FragPos);
                float attenuation = 1.0 / (distance * distance);
                vec3 lightDir = normalize((lightPos - FragPos));
                
                float diff = max(dot(lightDir, normal), 0.0);// * attenuation;
                vec3 diffuse = diff * pow(texture(diffuseMap, texCoords).rgb, vec3(2.2));

            	// specular
                
                
                //vec3 reflectDir = reflect(-lightDir, normal);
                vec3 halfwayDir = normalize((lightDir + viewDir));
            	float spec = pow(max(dot(normal, halfwayDir), 0), metallic * 255);//vec3 spec = FresnelSchlick(max(dot(normal, halfwayDir), 0), F0);//
                vec3 specular = vec3(.4) * spec * texture(specularMap, texCoords).rgb; // assuming bright white light color

            	if (allowAlpha == 1 && transparency != 0)
            		FragColor = vec4((ambient + diffuse + specular), transparency);//
            	else
            		FragColor = vec4(ambient + diffuse + specular, 1.0);//
            //vec3 I = normalize(FragPos - viewPos);
            //vec3 R = reflect(I, normalize(normal));
            //FragColor = vec4(texture(skybox, R).rgb, 1.0);
            }