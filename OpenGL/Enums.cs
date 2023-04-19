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

    public enum BufferTarget
    {
        ArrayBuffer = GL_ARRAY_BUFFER,
        CopyReadBuffer = GL_COPY_READ_BUFFER,
        CopyWriteBuffer = GL_COPY_WRITE_BUFFER,
        ElementArrayBuffer = GL_ELEMENT_ARRAY_BUFFER,
        PixelPackBuffer = GL_PIXEL_PACK_BUFFER,
        PixelUnpackBuffer = GL_PIXEL_UNPACK_BUFFER,
        ShaderStorageBuffer = GL_SHADER_STORAGE_BUFFER,
        TextureBuffer = GL_TEXTURE_BUFFER,
        TransformFeedbackBuffer = GL_TRANSFORM_FEEDBACK_BUFFER,
        UniformBuffer = GL_UNIFORM_BUFFER
    }

    public enum ShaderType
    {
        Lighting,
        PathTracing,
        FullBright
    }
}