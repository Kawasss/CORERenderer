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
    {
        color.z = 0.7;
    }

    if (fragPos3D.z > -0.001 * minimumz && fragPos3D.z < 0.001 * minimumz)
    {
        color.x = 0.7;
    }
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