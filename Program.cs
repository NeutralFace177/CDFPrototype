using CDFPrototype;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Reflection.Metadata;
using System.Xml.Linq;
using System.Numerics;

public class Program
{
    public static void Main()
    {
        Window window = new Window(2000,1200,"Sigma");
        window.Run();
    }
}

public class Window : GameWindow
{
    float[] vertices =
    {
        1f, 1f,  1, 1,
        -1f,1f,  0,1,
        -1f,-1f, 0,0,
        1f, -1f, 1, 0
    };

    int vertexBufferObject; 
    int vertexArrayObject;
    private Shader shader;
    int textureHandle;
    float[] textureData;

    public Window(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { ClientSize = (width, height), Title = title })
    {
        /*Vector3[] arr = Class1.Func(700,700);
        textureData = new float[arr.Length * 3];
        StreamWriter sw = new StreamWriter("C:\\Users\\Jacob\\Downloads\\TWOBLACKHOLESFROMMATH3.txt");
        for (int i = 0; i < arr.Length; i++)
        {
            textureData[i * 3] = arr[i].X/255f; 
            textureData[i * 3 + 1] = arr[i].Y / 255f;
            textureData[i * 3 + 2] = arr[i].Z/255f;
            sw.WriteLine("[" + arr[i].X + "," + arr[i].Y + "," + arr[i].Z + "],");
        }
        sw.Close();
        */
        textureData = new float[width * height];
    }

    protected override void OnLoad()
    {
        base.OnLoad();

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

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb32f, 700, 700, 0, PixelFormat.Rgb,PixelType.Float, textureData);

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
