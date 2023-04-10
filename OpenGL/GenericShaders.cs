using CORERenderer.shaders;

namespace CORERenderer.OpenGL
{
    public class GenericShaders : Rendering
    {
        private static Shader image2DShader, lightingShader, backgroundShader, gridShader, GenericLightingShader, solidColorQuadShader, arrowShader, pickShader, framebufferShader;

        public static Shader Image2D { get { return image2DShader; } }
        public static Shader Light { get { return lightingShader; } }
        public static Shader Background { get { return backgroundShader; } }
        public static Shader Grid { get { return gridShader; } }
        public static Shader GenericLighting { get { return GenericLightingShader; } }
        public static Shader Quad { get { return solidColorQuadShader; } }
        public static Shader Arrow { get { return arrowShader; } }
        public static Shader IDPicking { get { return pickShader; } }
        public static Shader Framebuffer { get { return framebufferShader; } }

        internal static void SetShaders()
        {
            image2DShader = new(image2DVertText, image2DFragText);
            lightingShader = new(lightVertText, lightFragText);
            backgroundShader = new(backgroundVertText, backgroundFragText);
            gridShader = new(gridVertText, gridFragText);
            if (shaderConfig == ShaderType.PathTracing)
                GenericLightingShader = new(defaultVertexShaderText, pathTracingFragText, pathTracingGeomText);
            else if (shaderConfig == ShaderType.Lighting)
                GenericLightingShader = new(defaultVertexShaderText, defaultLightingShaderText);
            else if (shaderConfig == ShaderType.FullBright)
                GenericLightingShader = new(defaultVertexShaderText, fullBrightFragText);
            solidColorQuadShader = new(quadVertText, quadFragText);
            arrowShader = new(arrowVertText, arrowFragText);
            pickShader = new(defaultVertexShaderText, quadFragText);
            framebufferShader = new(defaultFrameBufferVertText, defaultFrameBufferFragText);
        }

        //default shader source codes here
        private static string fullBrightFragText =
            """
            #version 430 core
            out vec4 FragColor;

            struct Material
            {
                sampler2D diffuse;
            };
            uniform Material material;

            in vec2 TexCoords;

            void main()
            {
                FragColor = texture(material.diffuse, TexCoords);
                if (FragColor.a < 0.1)
                    discard;
            }
            """;

        private static string pathTracingFragText =
            """
            #version 430 core
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

            in vec3 overrideColor;
            in vec3 Normal;

            void main()
            {
                FragColor = vec4(overrideColor, 1);
            }
            """;

        private static string pathTracingGeomText =
            """
            #version 430 core
            layout(triangles) in;
            layout(triangle_strip, max_vertices = 3) out;

            layout (std140, binding = 0) uniform Matrices
            {
            	mat4 projection;
            	mat4 view;
            };

            #define PI 3.14159265

            const float EPSILON = 0.0000001;
            out vec3 overrideColor;
            out vec3 Normal;
            struct Ray
            {
            	vec3 origin;
            	vec3 direction;
            };

            struct Light
            {
            	vec3 position;
            	vec3 color;
            };
            #define NUMBER_OF_LIGHTS 1
            uniform Light lights;

            in VS_OUT
            {
            	vec3 position;
            	vec3 normal;
            } gs_in[];

            uniform Ray RAY;
            uniform int isReflective;
            uniform vec3 emission;
            uniform float sampleAmount;

            bool RayIntersects(Ray ray, out vec3 intersection);
            vec3 Radiance(Ray ray, int depth, int includeEmissiveColor);
            float GetTFromIntersection(Ray ray, vec3 intersection);
            bool IntersectsSphere(Ray ray, out vec3 intersection);
            uint GetPCGHash(inout uint seed);
            float GetRandomFloat01();

            uint randomSeed;

            void main()
            {
            	float samples = sampleAmount;
            	if (samples > 7)
            	samples = 7;

            	randomSeed = 10546 * 1973 + 543543 * 9277 + 2699 | 1;

            	Normal = gs_in[0].normal;

            	int amount = 0;
            	vec3 localColor = vec3(0);
            	for (int i = 0; i < 7; i++, amount++)
            	{
            		vec3 intersection = vec3(0, 0, 0);
            		vec3 newIntersection = vec3(0, 0, 0);
            		Ray local;
            		local.origin = RAY.origin;
            		if (i < 3)
            			local.direction = gs_in[i].position - RAY.origin;
            		else if (i == 3)
            			local.direction = normalize((gs_in[1].position - gs_in[0].position) / 2);
            		else if (i == 4)
            			local.direction = normalize((gs_in[2].position - gs_in[1].position) / 2);
            		else if (i == 5)
            			local.direction = normalize((gs_in[0].position - gs_in[2].position) / 2);
            		else if (i == 6)
            			local.direction = normalize((gs_in[1].position - gs_in[0].position) / 2 + gs_in[2].position - gs_in[0].position);
            		bool success = RayIntersects(local, intersection);
            		if (!success) continue;

            		Ray newRay;
            		newRay.origin = intersection;
            		newRay.direction = -RAY.direction + 2 * Normal * dot(RAY.direction, Normal); //standard reflection formula

            		if (IntersectsSphere(newRay, newIntersection))
            			localColor += vec3(1, 1, 1);
            		else
            			localColor += vec3(0, 0, 0);

            		amount++;
            	}
            	overrideColor = localColor / amount;

            	gl_Position = gl_in[0].gl_Position;
            	EmitVertex();
            	gl_Position = gl_in[1].gl_Position;
            	EmitVertex();
            	gl_Position = gl_in[2].gl_Position;
            	EmitVertex();
            	EndPrimitive();
            }

            bool IntersectsSphere(Ray ray, out vec3 intersection)
            {
            	float rad = 3;
            	vec3 sphereCentre = RAY.origin; //set camera pos to ray origin out of laziness, making the camera a light
            	vec3 op = sphereCentre - ray.origin;
            	float t = 1e-4;
            	float eps = 1e-4;
            	float b = dot(op, ray.direction);
            	float det = b * b - dot(op, op) + rad * rad;
            	if (det < 0)
            	{
            		intersection = vec3(0);
            		return false;
            	}
            	else
            		det = sqrt(det);

            	float option1 = b - det;
            	float option2 = b + det;
            	float result = option1 > eps ? t : option2 > eps ? t : 0;

            	intersection = ray.origin + result * ray.direction;
            	return true;
            }

            bool RayIntersects(Ray ray, out vec3 intersection)
            {
            	vec3 vertex0 = gs_in[0].position.xyz;
            	vec3 vertex1 = gs_in[1].position.xyz;
            	vec3 vertex2 = gs_in[2].position.xyz;

            	vec3 edge0 = vertex1 - vertex0;
            	vec3 edge1 = vertex2 - vertex0;

            	vec3 h = cross(ray.direction, edge1);
            	float a = dot(edge0, h);

            	if (a > -EPSILON && a < EPSILON)
            	{
            		intersection = vec3(0);
            		return false;
            	}
            	float f = 1 / a;
            	vec3 s = ray.origin - vertex0;
            	float u = f * dot(s, h);

            	if (u < 0 || u > 1)
            	{
            		intersection = vec3(0);
            		return false;
            	}

            	vec3 q = cross(s, edge0);
            	float v = f * dot(ray.direction, q);

            	if (v < 0 || v > 1)
            	{
            		//intersection = vec3(0);
            		return false;
            	}

            	float t = f * dot(edge1, q);
            	if (t > EPSILON)
            	{
            		intersection = ray.origin + ray.direction * t;
            		return true;
            	}
            	else
            	{
            		intersection = vec3(0);
            		return false;
            	}
            }

            float GetTFromIntersection(Ray ray, vec3 intersection)
            {
            	return ((intersection - ray.origin) / ray.direction).x; 
            }

            //from https://github.com/BoyBaykiller/OpenTK-PathTracer/blob/master/OpenTK-PathTracer/res/shaders/PathTracing/compute.glsl
            uint GetPCGHash(inout uint seed)
            {
                seed = seed * 747796405u + 2891336453u;
                uint word = ((seed >> ((seed >> 28u) + 4u)) ^ seed) * 277803737u;
                return (word >> 22u) ^ word;
            }

            float GetRandomFloat01()
            {
                return float(GetPCGHash(randomSeed)) / 4294967296.0;
            }
            """;

        private static string arrowVertText =
            """
            #version 430 core
            layout (location = 0) in vec3 aPos;
            layout (location = 1) in vec3 aNormal;
            layout (location = 2) in vec2 aTexCoords;

            layout (std140, binding = 0) uniform Matrices
            {
            	mat4 projection;
            	mat4 view;
            };

            uniform mat4 model;

            void main() 
            {
            	gl_Position = vec4(aPos, 1) * model * view * projection;
            }
            """;

        private static string arrowFragText =
            """
            #version 430 core
            out vec4 FragColor;

            uniform vec3 color;

            void main() 
            {
            	FragColor = vec4(color, 1);
            }
            """;

        private static string lightVertText =
            """
            #version 430 core
            layout (std140, binding = 0) uniform Matrices
            {
            	mat4 projection;
            	mat4 view;
            	mat4 translationlessView;
            };

            vec3 coordinates[36] = vec3[](
            	vec3(-0.5, -0.5, -0.5), vec3(0.5, 0.5, -0.5), vec3(0.5, -0.5, -0.5),
                vec3(0.5, 0.5, -0.5), vec3(-0.5, -0.5, -0.5), vec3(-0.5, 0.5, -0.5),

            	vec3(-0.5, -0.5, 0.5), vec3(0.5, -0.5, 0.5), vec3(0.5, 0.5, 0.5),
                vec3(0.5, 0.5, 0.5), vec3(-0.5, 0.5, 0.5), vec3(-0.5, -0.5, 0.5),

            	vec3(-0.5, 0.5, 0.5), vec3(-0.5, 0.5, -0.5), vec3(-0.5, -0.5, -0.5),
                vec3(-0.5, -0.5, -0.5), vec3(-0.5, -0.5, 0.5), vec3(-0.5, 0.5, 0.5),

            	vec3(0.5, 0.5, 0.5), vec3(0.5, -0.5, -0.5), vec3(0.5, 0.5, -0.5),
                vec3(0.5, -0.5, -0.5), vec3(0.5, 0.5, 0.5), vec3(0.5, -0.5, 0.5),

            	vec3(-0.5, -0.5, -0.5), vec3(0.5, -0.5, -0.5), vec3(0.5, -0.5, 0.5),
                vec3(0.5, -0.5, 0.5), vec3(-0.5, -0.5, 0.5), vec3(-0.5, -0.5, -0.5),

            	vec3(-0.5, 0.5, -0.5), vec3(0.5, 0.5, 0.5), vec3(0.5, 0.5, -0.5),
                vec3(0.5, 0.5, 0.5), vec3(-0.5, 0.5, -0.5), vec3(-0.5, 0.5, 0.5)
            );

            uniform mat4 model;

            void main() 
            {
            	vec3 v1 = coordinates[gl_VertexID].xyz;
            	gl_Position = vec4(v1, 1.0) * model * view * projection;
            }
            """;

        private static string lightFragText =
            """
            #version 430 core
            out vec4 FragColor;

            void main() 
            {
            	FragColor = vec4(1.0);
            }
            """;

        private static string image2DVertText =
            """
            #version 430 core
            layout (location = 0) in vec4 vertex;

            out vec2 TexCoords;

            uniform mat4 projection;

            void main()
            {
                gl_Position = projection * vec4(vertex.xy, 0, 1);
                TexCoords = vertex.zw;
            }
            """;

        private static string image2DFragText =
            """
            #version 430 core
            out vec4 FragColor;

            in vec2 TexCoords;

            uniform sampler2D Texture;

            void main()
            {
            	FragColor = texture(Texture, TexCoords);
            }
            """;

        private static string gridVertText =
            """
            #version 430 core
            layout (std140, binding = 0) uniform Matrices
            {
            	mat4 projection;
            	mat4 view;
            	mat4 translationlessView;
            };

            out vec3 coor;
            out vec3 oPlayerPos;

            out mat4 oModel;
            out mat4 oView;
            out mat4 oProjection;

            uniform mat4 model;
            uniform vec3 playerPos;

            vec3 coordinates[6] = vec3[](
            	vec3(-1, 0, -1), vec3(1, 0, 1), vec3(1, 0, -1),
                vec3(1, 0, 1), vec3(-1, 0, -1), vec3(-1, 0, 1)
            );

            void main() 
            {
            	oModel = model;
            	oView = view;
            	oProjection = projection;
            	oPlayerPos = playerPos;

            	vec3 v1 = coordinates[gl_VertexID].xyz;
            	coor = v1;
            	gl_Position = vec4(v1, 1.0) * model * view * projection;
            }
            """;

        private static string gridFragText =
            """
            #version 430 core
            out vec4 gridColor;

            in mat4 oModel;
            in mat4 oView;
            in mat4 oProjection;

            in vec3 coor;
            in vec3 oPlayerPos;

            in vec3 nearPoint;
            in vec3 farPoint;

            float near = 0.01;
            float far = 100;
            float opacity;

            vec4 grid(vec3 fragPos3D, float scale, bool drawAxis) {
                vec2 coord = fragPos3D.xz * scale;
                vec2 derivative = fwidth(coord);
                vec2 grid = abs(fract(coord - 0.5) - 0.5) / derivative;
                float line = min(grid.x, grid.y);
                float minimumz = min(derivative.y, 1);
                float minimumx = min(derivative.x, 1);
                vec4 color = vec4(0.1, 0.1, 0.1, 1.0 - min(line, 1.0));

                if (fragPos3D.x > -0.001 * minimumx && fragPos3D.x < 0.001 * minimumx)
                    color.z = 0.7;

                if (fragPos3D.z > -0.001 * minimumz && fragPos3D.z < 0.001 * minimumz)
                    color.x = 0.7;

                return color;
            }

            void main() 
            {
                vec3 coor3D = (vec4(coor, 1) * oModel * oView * oProjection).xyz;

                float Distance = distance(oPlayerPos, coor);
                float opacity = clamp(Distance / length(oPlayerPos + coor3D) * 2, 0, 1);

                gridColor = grid(coor, 1000, true) + grid(coor, 100, true);
                gridColor.a *= opacity;
            }
            """;

        private static string backgroundVertText =
            """
            #version 430 core

            out vec3 TexCoords;

            layout (std140, binding = 0) uniform Matrices
            {
                mat4 projection;
                mat4 view;
                mat4 translationlessView;
            };

            vec3 coordinates[36] = vec3[](
            	    vec3(-1.0f,  1.0f, -1.0f),
                    vec3(-1.0f, -1.0f, -1.0f),
                    vec3( 1.0f, -1.0f, -1.0f),
                    vec3( 1.0f, -1.0f, -1.0f),
                    vec3( 1.0f,  1.0f, -1.0f),
                    vec3(-1.0f,  1.0f, -1.0f),

                    vec3(-1.0f, -1.0f,  1.0f),
                    vec3(-1.0f, -1.0f, -1.0f),
                    vec3(-1.0f,  1.0f, -1.0f),
                    vec3(-1.0f,  1.0f, -1.0f),
                    vec3(-1.0f,  1.0f,  1.0f),
                    vec3(-1.0f, -1.0f,  1.0f),

                    vec3( 1.0f, -1.0f, -1.0f),
                    vec3( 1.0f, -1.0f,  1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3( 1.0f,  1.0f, -1.0f),
                    vec3( 1.0f, -1.0f, -1.0f),

                    vec3(-1.0f, -1.0f,  1.0f),
                    vec3(-1.0f,  1.0f,  1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3( 1.0f, -1.0f,  1.0f),
                    vec3(-1.0f, -1.0f,  1.0f),

                    vec3(-1.0f,  1.0f, -1.0f),
                    vec3( 1.0f,  1.0f, -1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3(-1.0f,  1.0f,  1.0f),
                    vec3(-1.0f,  1.0f, -1.0f),

                    vec3(-1.0f, -1.0f, -1.0f),
                    vec3(-1.0f, -1.0f,  1.0f),
                    vec3( 1.0f, -1.0f, -1.0f),
                    vec3( 1.0f, -1.0f, -1.0f),
                    vec3(-1.0f, -1.0f,  1.0f),
                    vec3( 1.0f, -1.0f,  1.0f)
            );

            void main()
            {
            	TexCoords = coordinates[gl_VertexID];
                vec4 temp =  vec4(TexCoords, 1) * translationlessView * projection;
            	gl_Position = temp.xyww;
            }
            """;

        private static string backgroundFragText =
            """
            #version 430 core
            out vec4 FragColor;
            in vec3 TexCoords;

            uniform samplerCube environmentMap;

            void main()
            {		
                vec3 envColor = texture(environmentMap, TexCoords).rgb;
                FragColor = vec4(envColor, 1.0);
            }
            """;

        private static string quadVertText =
            """
            #version 430 core
            layout (location = 0) in vec2 aPos;

            uniform mat4 projection;

            void main()
            {
            	gl_Position = projection * vec4(aPos.xy, 0, 1);
            }
            """;

        private static string quadFragText =
            """
            #version 430 core
            out vec4 FragColor;

            uniform vec3 color;
            uniform float alpha;

            void main()
            {
            	float alpha2 = alpha;
            	if (alpha == 0)
            		alpha2 = 1;

            	FragColor = vec4(color, alpha2);
            }
            """;

        private static string defaultFrameBufferVertText =
            """
            #version 430 core
            layout (location = 0) in vec2 aPos;
            layout (location = 1) in vec2 aTexCoords;

            out vec2 TexCoords;

            void main()
            {
            	gl_Position = vec4(aPos.x, aPos.y, 0, 1);
            	TexCoords = aTexCoords;
            }
            """;

        private static string defaultFrameBufferFragText =
            """
            #version 430 core
            out vec4 FragColor;

            in vec2 TexCoords;

            uniform sampler2D screenTexture;

            uniform int useVignette;
            uniform float vignetteStrength;

            uniform int useChromaticAberration;
            uniform vec3 chromAberIntensities;

            void main()
            {
            	float distanceFromCentreOfScreen = length(TexCoords - 0.5);

            	vec4 color;
            	if (useChromaticAberration == 1)
            	{
            		vec2 rUV = TexCoords + chromAberIntensities.x * (TexCoords - 0.5);
            		vec2 gUV = TexCoords + chromAberIntensities.y * (TexCoords - 0.5);
            		vec2 bUV = TexCoords + chromAberIntensities.z * (TexCoords - 0.5);

            		color.r = texture(screenTexture, rUV).r;
            		color.g = texture(screenTexture, gUV).g;
            		color.b = texture(screenTexture, bUV).b;
            	}
            	else
            		color = texture(screenTexture, TexCoords);

            	float vignetteColor = 0;
            	if (useVignette == 1)
            		vignetteColor = (distanceFromCentreOfScreen) * vignetteStrength;

            	color -= vignetteColor;
            	color.a = 1;

            	FragColor = color;
            }
            """;

        private static string defaultVertexShaderText =
            """
            #version 430 core
            layout (location = 0) in vec3 aPos;
            layout (location = 1) in vec3 aNormal;
            layout (location = 2) in vec2 aTexCoords;

            layout (std140, binding = 0) uniform Matrices
            {
            	mat4 projection;
            	mat4 view;
            };

            out VS_OUT
            {
            	vec3 position;
            	vec3 normal;
            } vs_out;

            out mat4 Model;

            uniform mat4 model;
            out vec2 TexCoords;
            out vec3 FragPos;
            out vec3 Normal;

            void main() 
            {
            	FragPos = (vec4(aPos, 1.0) * model).xyz;
            	Normal = mat3(transpose(inverse(model))) * aNormal; //way more efficient if calculated on CPU
            	vs_out.normal = mat3(transpose(inverse(model))) * aNormal;
            	TexCoords = aTexCoords;
            	Model = model;
            	vs_out.position = FragPos;

            	gl_Position = vec4(FragPos, 1) * view * projection;
            }
            """;

        private static string defaultLightingShaderText =
            """
            #version 430 core
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

            uniform vec3 viewPos;
            uniform float distanceObject;
            uniform float transparency;
            uniform int allowAlpha;
            uniform vec3 overrideColor;
            uniform int hasNormalMap;

            in vec2 TexCoords;

            in vec3 Normal;
            in vec3 FragPos;

            vec3 getNormalFromMap()
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
            }

            void main()
            {
            	vec4 fullColor = texture(material.Texture, TexCoords);
            	if (fullColor.a < 0.1)
            		discard;

            	vec3 color = fullColor.rgb;

            	// ambient
                	vec3 ambient = .2 * color;

            	// diffuse
                	vec3 lightDir = normalize(pointLights[0].position - FragPos);
                	vec3 normal = normalize(Normal);//getNormalFromMap();
                	float diff = max(dot(lightDir, normal), 0.0);
                	vec3 diffuse = vec3(.8) * diff * pow(texture(material.diffuse, TexCoords).rgb, vec3(2.2));

            	// specular
                	vec3 viewDir = normalize(viewPos - FragPos);
                	vec3 reflectDir = reflect(-lightDir, normal);
                	vec3 halfwayDir = normalize(lightDir + viewDir);
            	float spec = pow(max(dot(normal, halfwayDir), 0), 32);
                	vec3 specular = vec3(1) * spec * texture(material.specular, TexCoords).rgb; // assuming bright white light color
            	if (allowAlpha == 1 && transparency != 0)
            		FragColor = vec4(ambient + diffuse + specular, transparency);
            	else
            		FragColor = vec4(ambient + diffuse + specular, 1.0);
            	if (overrideColor != vec3(0, 0, 0))
            		FragColor = vec4(overrideColor, fullColor.a);
            }
            """;
    }
}