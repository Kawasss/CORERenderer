#version 460 core
in vec2 texCoord;
in vec4 FragColor;

out vec4 gridColor;

void main() {
    vec2 uv = texCoord/ 600;
    float size = 1.0/8.0;   // size of the tile
    float edge = size/32.0; // size of the edge
    float face_tone = 0.9; // 0.9 for the face of the tile
    float edge_tone = 0.5; // 0.5 for the edge
    uv = sign(vec2(edge) - mod(uv, size));
    gridColor = vec4(face_tone - sign(uv.x + uv.y + 2.0) * (face_tone - edge_tone));
}