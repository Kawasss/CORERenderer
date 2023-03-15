#version 460 core
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
	int samples = 4;

	randomSeed = 10546 * 1973 + 543543 * 9277 + 2699 | 1;
	
	Normal = gs_in[0].normal;
	
	int amount = 0;
	vec3 localColor = vec3(0);
	for (float j = -3; j < 3; j += 1 / sampleAmount, amount++)
		for (float i = -3; i < 3; i += 1 / sampleAmount, amount++)
		{
			vec3 intersection = vec3(0, 0, 0);
			vec3 newIntersection = vec3(0, 0, 0);
			Ray local;
			local.origin = vec3(RAY.origin.x + i, RAY.origin.y + j, RAY.origin.z);
			local.direction = RAY.direction;
			bool success = RayIntersects(local, intersection);
			if (!success) continue;
		
			Ray newRay;
			newRay.origin = intersection;
			newRay.direction = -RAY.direction + 2 * Normal * dot(RAY.direction, Normal); //standard reflection formula
		
			if (IntersectsSphere(newRay, newIntersection))
				localColor += vec3(1, 1, 1);
			else
				localColor += vec3(0, 0, 0);
		}
	overrideColor = localColor / amount * 10;

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

vec3 Radiance(Ray ray, int depth, int includeEmissiveColor)
{
vec3 Color;
while (depth < 10)
{
	vec3 intersection;
	if (!RayIntersects(ray, intersection))
		return vec3(0);

	if (depth > 10)
		return vec3(0);

	vec3 normal = gs_in[0].normal;
	vec3 orientedNormal = dot(normal, ray.direction) < 0 ? normal : -normal; //properly orient the normal (dont know if this is actually necessary)
	vec3 color = vec3(1, 0, 1); //could sample color from textures, but this is more for debugging

	float maxReflection = color.r > color.g && color.r > color.b ? color.r : color.g > color.b ? color.g : color.b; //get the max amount it can reflect
	if (depth + 1 > 5 || maxReflection != 0)
		if(GetRandomFloat01() < maxReflection)
			color *= 1 / maxReflection;
		else 
			return emission * includeEmissiveColor;

	if (isReflective == 0)
	{ //create a random ray
		float randomAngle = 2 * PI * GetRandomFloat01();
		float q = GetRandomFloat01();
		float distanceFromCenter = sqrt(q);
		//orthonormal coordinates system
		vec3 w = orientedNormal;
		vec3 u = normalize(cross((abs(w.x) > 0.1 ? vec3(0, 1, 0) : vec3(1)), w)); //vec3(0.1)???
		vec3 v = cross(w, u);
		vec3 randomReflection = normalize(u * cos(randomAngle) * distanceFromCenter + v * sin(randomAngle) * distanceFromCenter + w * sqrt(1 - q)); //sample unit hemisphere

		vec3 e;
		for (int i = 0; i < NUMBER_OF_LIGHTS; i++)
		{
			vec3 sw = lights.position - intersection;
			vec3 su = normalize(cross(abs(sw.x) > 0.1 ? vec3(0, 1, 0) : vec3(1), sw));
			vec3 sv = cross(sw, su);

			float maxCosA = dot(intersection - lights.position, vec3(1)); //unnecessary??
			float eps1 = GetRandomFloat01();
			float eps2 = GetRandomFloat01();
			float cosA = 1 - eps1 + eps1 * maxCosA;
			float sinA = sqrt(1 - cosA * cosA);

			float phi = 2 * PI * eps2;
			vec3 l = normalize(su * cos(phi) * sinA + sv * sin(phi) * sinA + sw * cosA);

			Ray new;
			new.origin = intersection;
			new.direction = l;
			vec3 newIntersection;
			if (RayIntersects(new, newIntersection))
			{
				float omega = 2 * PI * (1 - maxCosA);
				e += color * (lights.color * dot(l, orientedNormal) * omega) * (1 / PI);
			}
		}
		//Ray newRay;
		//newRay.origin = intersection;
		//newRay.direction = randomReflection;
		//vec3 newIntersection;
		
		//return emission * includeEmissiveColor + color * Radiance(newRay, depth + 1, 0);
		depth++;
		ray.origin = intersection;
		ray.direction = randomReflection;
		Color = color;
	}
	}
	return emission * includeEmissiveColor + Color;

	return vec3(0);
}