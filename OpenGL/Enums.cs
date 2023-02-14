using static CORERenderer.OpenGL.GL;

namespace CORERenderer.OpenGL
{
    public enum PrimitiveType
    {
        Triangles = GL_TRIANGLES,
        TrianglesAdjacency = GL_TRIANGLES_ADJACENCY,
        TriangleStrip = GL_TRIANGLE_STRIP,
        TriangleStripAdjacency = GL_LINE_STRIP_ADJACENCY,
        TriangleFan = GL_TRIANGLE_FAN,

        Lines = GL_LINES,
        LinesAdjacency = GL_LINES_ADJACENCY,
        LineStripAdjacency = GL_LINE_STRIP_ADJACENCY,

        LineStrip = GL_LINE_STRIP,
        LineLoop = GL_LINE_LOOP,
        Points = GL_POINTS
    }

    public enum GLType
    {
        Float = GL_FLOAT,
        Int = GL_INT,
        UnsingedInt = GL_UNSIGNED_INT,
        Byte = GL_BYTE,
        UnsignedByte = GL_UNSIGNED_BYTE,
        Double = GL_DOUBLE,
        Short = GL_SHORT,
        UnsignedShort = GL_UNSIGNED_SHORT
    }
}
