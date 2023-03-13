#version 460 core
layout(points) in;

const float EPSILON = 0.0000001;
out vec3 overrideColor;
struct Ray
{
	vec3 origin;
	vec3 direction;
};

uniform Ray ray;

bool RayIntersects(Ray ray, out vec3 intersection);

void main()
{
	Ray localRay;
	localRay.origin = vec3(0);
	localRay.direction = vec3(0, 1, 0);
	vec3 intersection;
	bool success = RayIntersects(localRay, intersection);
	if (success)
		overrideColor = vec3(0, 1, 0);
	else
		overrideColor = vec3(1, 0, 0);
}

bool RayIntersects(Ray ray, out vec3 intersection)
{
	vec3 vertex0 = gl_in[0].gl_Position.xyz;
	vec3 vertex1 = gl_in[1].gl_Position.xyz;
	vec3 vertex2 = gl_in[2].gl_Position.xyz;

	vec3 edge0 = vertex1 - vertex0;
	vec3 edge1 = vertex2 - vertex0;

	vec3 h = cross(ray.direction, edge1);
	float a = dot(edge0, h);

	if (a > -EPSILON && a < EPSILON)
		return false;

	float f = 1 / a;
	vec3 s = ray.origin - vertex0;
	float u = f * dot(s, h);

	if (u < 0 || u > 1)
		return false;

	vec3 q = cross(s, edge0);
	float v = f * dot(ray.direction, q);

	if (v < 0 || v > 1)
		return false;

	float t = f * dot(edge1, q);
	if (t > EPSILON)
	{
		intersection = ray.origin + ray.direction * t;
		return true;
	}
	else
		return false;
}