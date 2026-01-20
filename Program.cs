using CDFPrototype;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Reflection.Metadata;
using System.Xml.Linq;

public class Program
{
    public static void Main()
    {
        Window window = new Window(800,800,"Sigma");
        window.Run();
    }
}

public class Window : GameWindow
{
    float[] vertices =
    {
        1f, 1f,  1, 1,
        -1f,1f,  -1,1,
        -1f,-1f, -1,-1,
        1f, -1f, 1, -1
    };

    int vertexBufferObject; 
    int vertexArrayObject;
    private Shader shader;
    int textureHandle;
    float[] textureData;

    public Window(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { ClientSize = (width, height), Title = title })
    {
        textureData = new float[800 * 800];
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        for (int i = 0; i < 800 * 800; i++)
        {
            textureData[i] = (((float)i) / 1000.0f) % 1.0f;
            //Console.WriteLine((((float)i) / 1000.0f) % 1.0f);
        }

        shader = new Shader("shaders/vert.glsl", "shaders/frag.glsl");
        textureHandle = GL.GenTexture();

        vertexBufferObject = GL.GenBuffer();

        vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(vertexArrayObject);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        GL.BindTexture(TextureTarget.Texture2D, 1);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, textureHandle);

        GL.UseProgram(shader.handle);
        GL.Uniform1(GL.GetUniformLocation(shader.handle, "texture1"), 0);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, 800, 800, 0, PixelFormat.Red,PixelType.Float, textureData);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        shader.Use();
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        shader.Dispose();
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        shader.Use();
        GL.BindVertexArray(vertexArrayObject);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, 1);
        GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

        SwapBuffers();
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {

    }

}
