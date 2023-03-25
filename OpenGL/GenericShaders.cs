using CORERenderer.Main;
using CORERenderer.shaders;
using System.Runtime.CompilerServices;

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
            image2DShader = new(true, image2DVertText, image2DFragText);
            lightingShader = new(true, lightVertText, lightFragText);
            backgroundShader = new(true, backgroundVertText, backgroundFragText);
            gridShader = new(true, gridVertText, gridFragText);
            GenericLightingShader = shaderConfig == ShaderType.PathTracing ? new($"{COREMain.pathRenderer}\\shaders\\shader.vert", $"{COREMain.pathRenderer}\\shaders\\lighting.frag", $"{COREMain.pathRenderer}\\shaders\\pathTracing.geom") : new(true, defaultVertexShaderText, defaultLightingShaderText);
            solidColorQuadShader = new(true, quadVertText, quadFragText);
            arrowShader = new($"{COREMain.pathRenderer}\\shaders\\Arrow.vert", $"{COREMain.pathRenderer}\\shaders\\Arrow.frag");
            pickShader = new($"{COREMain.pathRenderer}\\shaders\\shader.vert", $"{COREMain.pathRenderer}\\shaders\\SolidColor.frag");
            framebufferShader = new(true, defaultFrameBufferVertText, defaultFrameBufferFragText);
        }

        //default shader source codes here
        private static string lightVertText =
            """
            #version 460 core
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
            #version 460 core
            out vec4 FragColor;

            void main() 
            {
            	FragColor = vec4(1.0);
            }
            """;

        private static string image2DVertText =
            """
            #version 460 core
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
            #version 460 core
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
            #version 460 core
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
            #version 460 core
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
            #version 460 core

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
            #version 460 core
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
            #version 460 core
            layout (location = 0) in vec2 aPos;

            uniform mat4 projection;

            void main()
            {
            	gl_Position = projection * vec4(aPos.xy, 0, 1);
            }
            """;

        private static string quadFragText =
            """
            #version 460 core
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
            #version 460 core
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
            #version 460 core
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
            #version 460 core
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
            	vs_out.normal = Normal;
            	TexCoords = aTexCoords;
            	Model = model;
            	vs_out.position = FragPos;

            	gl_Position = vec4(FragPos, 1) * view * projection;
            }
            """;

        private static string defaultLightingShaderText =
            """
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
                	vec3 ambient = .1 * color;

            	// diffuse
                	vec3 lightDir = normalize(pointLights[0].position - FragPos);
                	vec3 normal = normalize(Normal);//getNormalFromMap();
                	float diff = max(dot(lightDir, normal), 0.0);
                	vec3 diffuse = vec3(0.4) * diff * pow(texture(material.diffuse, TexCoords).rgb, vec3(2.2));

            	// specular
                	vec3 viewDir = normalize(viewPos - FragPos);
                	vec3 reflectDir = reflect(-lightDir, normal);
                	vec3 halfwayDir = normalize(lightDir + viewDir);
            	float spec = pow(max(dot(normal, halfwayDir), 0), 32);
                	vec3 specular = vec3(0.7) * spec * texture(material.specular, TexCoords).rgb; // assuming bright white light color
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