using COREMath;
using static CORERenderer.OpenGL.GL;

namespace CORERenderer.OpenGL
{
    public static class Color
    {
        public static Vector3 White { get => new(1, 1, 1); }
        public static Vector3 Black { get => Vector3.Zero; }
        public static Vector3 Red   { get => Vector3.UnitVectorX; }
        public static Vector3 Green { get => Vector3.UnitVectorY; }
        public static Vector3 Blue  { get => Vector3.UnitVectorZ; }
    }

    /// <summary>
    /// determines the quality of certain render actions, like textures.
    /// Maximum uses 6 times the given resolution,
    /// Ultra uses 4 times the given resolution,
    /// High uses 2 times the given resolution,
    /// Default uses the given resolution,
    /// Medium uses half of the given resolution,
    /// Low uses a quarter of the given resolution,
    /// Minimum uses one sixteenth of the given resolution,
    /// Lowest uses one sixty fourth of the given resolution
    /// </summary>
    public static class TextureQuality
    {
        public const float Maximum = 0.16667f;
        public const float Ultra = 0.25f;
        public const float High = 0.5f;
        public const float Default = 1;
        public const float Medium = 2;
        public const float Low = 4;
        public const float Minimum = 16;
        public const float Lowest = 64;
    }

    /// <summary>
    /// determines the quality of certain render actions, like reflections.
    /// Maximum uses 4 times the given resolution,
    /// Ultra uses 2 times the given resolution,
    /// High uses the given resolution,
    /// Default uses 3/4 of the given resolution,
    /// Medium uses 1/4 of the given resolution,
    /// Low uses a 1/8 of the given resolution,
    /// Minimum uses 1/32 of the given resolution,
    /// Lowest uses 1/128 of the given resolution
    /// </summary>
    public static class ShadowQuality
    {
        public const float Maximum = 0.25f;
        public const float Ultra = 0.5f;
        public const float High = 1f;
        public const float Default = 1.5f;
        public const float Medium = 4;
        public const float Low = 8;
        public const float Minimum = 32;
        public const float Lowest = 128;
    }

    /// <summary>
    /// determines the quality of certain render actions, like reflections.
    /// Maximum uses 4 times the given resolution,
    /// Ultra uses 2 times the given resolution,
    /// High uses the given resolution,
    /// Default uses 3/4 of the given resolution,
    /// Medium uses 1/4 of the given resolution,
    /// Low uses a 1/8 of the given resolution,
    /// Minimum uses 1/32 of the given resolution,
    /// Lowest uses 1/128 of the given resolution
    /// </summary>
    public static class ReflectionQuality
    {
        public const float Maximum = 0.25f;
        public const float Ultra = 0.5f;
        public const float High = 1f;
        public const float Default = 1.5f;
        public const float Medium = 4;
        public const float Low = 8;
        public const float Minimum = 32;
        public const float Lowest = 128;
    }

    public enum Usage 
    {
        StreamDraw = GL_STREAM_DRAW,
        StreamRead = GL_STREAM_READ,
        StreamCopy = GL_STREAM_COPY,
        StaticDraw = GL_STATIC_DRAW,
        StaticRead = GL_STATIC_READ,
        StaticCopy = GL_STATIC_COPY,
        DynamicDraw = GL_DYNAMIC_DRAW,
        DynamicRead = GL_DYNAMIC_READ,
        DynamicCopy = GL_DYNAMIC_COPY
    }

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

    public enum Image2DTarget
    { 
        Texture2D = GL_TEXTURE_2D,
        ProxyTexture2D = GL_PROXY_TEXTURE_2D,
        Texture1DArray = GL_TEXTURE_1D_ARRAY,
        ProxyTexture1DArray = GL_PROXY_TEXTURE_1D_ARRAY,
        TextureRectangle = GL_TEXTURE_RECTANGLE,
        ProxyTextureRectangle = GL_PROXY_TEXTURE_RECTANGLE,
        TextureCubeMapPositiveX = GL_TEXTURE_CUBE_MAP_POSITIVE_X,
        TextureCubeMapNegativeX = GL_TEXTURE_CUBE_MAP_NEGATIVE_X,
        TextureCubeMapPositiveY = GL_TEXTURE_CUBE_MAP_POSITIVE_Y,
        TextureCubeMapNegativeY = GL_TEXTURE_CUBE_MAP_NEGATIVE_Y,
        TextureCubeMapPositiveZ = GL_TEXTURE_CUBE_MAP_POSITIVE_Z,
        TextureCubeMapNegativeZ = GL_TEXTURE_CUBE_MAP_NEGATIVE_Z,
        ProxyTextureCubeMap = GL_PROXY_TEXTURE_CUBE_MAP
    }

    public enum ActiveTexture
    {
        Texture0 = GL_TEXTURE0,
        Texture1 = GL_TEXTURE1,
        Texture2 = GL_TEXTURE2,
        Texture3 = GL_TEXTURE3,
        Texture4 = GL_TEXTURE4,
        Texture5 = GL_TEXTURE5,
        Texture6 = GL_TEXTURE6,
        Texture7 = GL_TEXTURE7,
        Texture8 = GL_TEXTURE8,
        Texture9 = GL_TEXTURE9,

        Texture10 = GL_TEXTURE10,
        Texture11 = GL_TEXTURE11,
        Texture12 = GL_TEXTURE12,
        Texture13 = GL_TEXTURE13,  
        Texture14 = GL_TEXTURE14,
        Texture15 = GL_TEXTURE15,
        Texture16 = GL_TEXTURE16,
        Texture17 = GL_TEXTURE17,
        Texture18 = GL_TEXTURE18,
        Texture19 = GL_TEXTURE19,
        Texture20 = GL_TEXTURE20,

        Texture21 = GL_TEXTURE21,
        Texture22 = GL_TEXTURE22,
        Texture23 = GL_TEXTURE23,
        Texture24 = GL_TEXTURE24,
        Texture25 = GL_TEXTURE25,
        Texture26 = GL_TEXTURE26,
        Texture27 = GL_TEXTURE27,
        Texture28 = GL_TEXTURE28,
        Texture29 = GL_TEXTURE29,
        Texture30 = GL_TEXTURE30,

        Texture31 = GL_TEXTURE31
    }

    public enum ShaderType
    {
        PBR,
        PathTracing,
        FullBright
    }
}