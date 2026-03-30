using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Reflection.Metadata;
using System.Xml.Linq;
using System.Numerics;
using CFDPrototype.util;
using CFDPrototype;
using System.Text;

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
    int textureHandle1;
    int textureHandle2;
    float[] textureData;
    Grid grid;
    int gWidth;
    int gHeight;
    int zuh;

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
        float[,] a = { { 5.0f, 4.0f , 3.0f}, { 9.0f, 6.0f , 1.0f} , { 7.0f, 8.0f, 2.0f} };
        Matrix sigma = new Matrix(a);
        Console.WriteLine(sigma.ToString());
        Console.WriteLine(sigma.SwapColumn(1, 3));
        textureData = new float[width * height*3];
        gWidth = 200;
        gHeight = 120;
        grid = new Grid(gWidth, gHeight);
        zuh = 0;
        for (int i = 0; i < gWidth; i++)
        {
            for (int j = 0; j < gHeight; j++)
            {
                textureData[(gWidth * j + i) * 3] = grid.u[i, j];
                textureData[(gWidth * j + i) * 3 + 1] = grid.v[i, j];
                textureData[(gWidth * j + i) * 3 + 2] = grid.d[i, j] / 2f;
            }
        }

    }
    static string FloatToBinary(float f)
    {
        StringBuilder sb = new StringBuilder();
        Byte[] ba = BitConverter.GetBytes(f);
        foreach (Byte b in ba)
            for (int i = 0; i < 8; i++)
            {
                sb.Insert(0, ((b >> i) & 1) == 1 ? "1" : "0");
            }
        string s = sb.ToString();
        string r = s.Substring(0, 1) + " " + s.Substring(1, 8) + " " + s.Substring(9); //sign exponent mantissa
        return r;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        shader = new Shader("Shaders/vert.glsl", "Shaders/frag.glsl");
        textureHandle = GL.GenTexture();
        textureHandle1 = GL.GenTexture();
        textureHandle2 = GL.GenTexture();

        vertexBufferObject = GL.GenBuffer();

        vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(vertexArrayObject);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));;

        //tex1
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, textureHandle);
        GL.UseProgram(shader.handle);
        GL.Uniform1(GL.GetUniformLocation(shader.handle, "texture1"), 0);
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, gWidth, gHeight, 0, PixelFormat.Red, PixelType.Float, grid.u);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        //tex2
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, textureHandle1);
        GL.Uniform1(GL.GetUniformLocation(shader.handle, "texture2"), 1);
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, gWidth, gHeight, 0, PixelFormat.Red, PixelType.Float, grid.v);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        //tex3
        GL.ActiveTexture(TextureUnit.Texture2);
        GL.BindTexture(TextureTarget.Texture2D, textureHandle2);
        GL.Uniform1(GL.GetUniformLocation(shader.handle, "texture3"), 2);
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, gWidth, gHeight, 0, PixelFormat.Red, PixelType.Float, grid.d);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
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
        GL.BindTexture(TextureTarget.Texture2D, textureHandle);
        GL.Uniform1(GL.GetUniformLocation(shader.handle, "texture1"), 0);
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, gWidth, gHeight, 0, PixelFormat.Red, PixelType.Float, grid.u);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, textureHandle1);
        GL.Uniform1(GL.GetUniformLocation(shader.handle, "texture2"), 1);
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, gWidth, gHeight, 0, PixelFormat.Red, PixelType.Float, grid.v);

        GL.ActiveTexture(TextureUnit.Texture2);
        GL.BindTexture(TextureTarget.Texture2D, textureHandle2);
        GL.Uniform1(GL.GetUniformLocation(shader.handle, "texture3"), 2);
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, gWidth, gHeight, 0, PixelFormat.Red, PixelType.Float, grid.d);
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
        zuh++;
        Console.WriteLine("t"+zuh);
        grid.TimeStep(0.1f);
    }
}
