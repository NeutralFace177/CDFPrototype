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
    ComputeShader computeShader;
    int textureHandle;
    int compTextureHandle;
    Field2D[,] compShaderDataIn;
    Field2D[,] compShaderDataOut;
    Field2D sigmaa;
    int ssbo;
    int ssbo1;
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
        gWidth = 300;
        gHeight = 180;
        grid = new Grid(gWidth, gHeight);
        compShaderDataIn = new Field2D[gWidth, gHeight];
        compShaderDataOut = new Field2D[gWidth, gHeight];
        sigmaa = new Field2D(0.1f, 0.2f, 0.3f, 0.4f, 0.5f);
        grid.StoreToField2D(compShaderDataIn);
        zuh = 0;
        for (int i = 0; i < gWidth; i++)
        {
            for (int j = 0; j < gHeight; j++)
            {
         //       textureData[(gWidth * j + i) * 3] = grid.u[i, j];
          //      textureData[(gWidth * j + i) * 3 + 1] = grid.v[i, j];
          //      textureData[(gWidth * j + i) * 3 + 2] = grid.d[i, j] / 2f;
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

    unsafe protected override void OnLoad()
    {
        base.OnLoad();

        shader = new Shader("Shaders/vert.glsl", "Shaders/frag.glsl");
        computeShader = new ComputeShader("Shaders/compute.glsl");
        textureHandle = GL.GenTexture();
        compTextureHandle = GL.GenTexture();
        GL.CreateBuffers(1, out ssbo);
        GL.CreateBuffers(1, out ssbo1);
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo);
        unsafe
        {
            fixed (Field2D* ptr = &compShaderDataIn[0,0]) {
                GL.BufferData(BufferTarget.ShaderStorageBuffer, compShaderDataIn.Length * sizeof(Field2D), (IntPtr)ptr, BufferUsageHint.DynamicCopy);
            }
        }

        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, ssbo);
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo1);
        unsafe
        {
            fixed (Field2D* ptr = &compShaderDataOut[0,0])
            {
                GL.BufferData(BufferTarget.ShaderStorageBuffer, compShaderDataOut.Length * sizeof(Field2D), (IntPtr)ptr, BufferUsageHint.DynamicRead);
            }
        }
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, ssbo1);
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

        vertexBufferObject = GL.GenBuffer();

        vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(vertexArrayObject);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, textureHandle);
        GL.UseProgram(shader.handle);
        GL.Uniform1(GL.GetUniformLocation(shader.handle, "texture1"), 1);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb32f, gWidth, gHeight, 0, PixelFormat.Rgb,PixelType.Float, textureData);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, compTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, gWidth, gHeight, 0, PixelFormat.Rgba, PixelType.Float, new IntPtr());
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.BindImageTexture(1, compTextureHandle, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);

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
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo);
        IntPtr ptr = GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.WriteOnly);
        unsafe
        {
            fixed (void* dataPtr = &compShaderDataIn[0,0])
            {
                System.Buffer.MemoryCopy(dataPtr, ptr.ToPointer(), compShaderDataIn.Length * sizeof(Field2D), compShaderDataIn.Length * sizeof(Field2D));
            }
        }
        GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
        int block_index = GL.GetProgramResourceIndex(computeShader.handle, ProgramInterface.ShaderStorageBlock, "shader_data");
        int ssbo_binding_point_index = 2;
        GL.ShaderStorageBlockBinding(computeShader.handle, block_index, ssbo_binding_point_index);

        computeShader.Use();
        GL.DispatchCompute(gWidth, gHeight, 1);
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo1);
        IntPtr ptr1 = GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.ReadWrite);
        unsafe
        {
            fixed (void* dataPtr = &compShaderDataOut[0,0])
            {
                fixed (void* dataPtr2 = &compShaderDataIn[0, 0])
                {
                    System.Buffer.MemoryCopy(ptr1.ToPointer(), dataPtr, compShaderDataOut.Length * sizeof(Field2D), compShaderDataOut.Length * sizeof(Field2D));
                    System.Buffer.MemoryCopy(dataPtr, dataPtr2, compShaderDataOut.Length * sizeof(Field2D), compShaderDataOut.Length * sizeof(Field2D));
                }
            }
        }
        GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
        int block_index1 = GL.GetProgramResourceIndex(computeShader.handle, ProgramInterface.ShaderStorageBlock, "out_data");
        int ssbo1_binding_point_index = 3;
        GL.ShaderStorageBlockBinding(computeShader.handle,block_index1, ssbo1_binding_point_index);


        shader.Use();
        GL.BindVertexArray(vertexArrayObject);
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, compTextureHandle);
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
        Console.WriteLine("step:" + zuh + " t:" + (zuh * 0.0006f).ToString("0.0000") + "                fps:" + (1 / e.Time).ToString("#.#"));
      //  grid.TimeStep(0.0006f);
        if (0 == 1)
        {
            for (int i = 0; i < gWidth; i++)
            {
                for (int j = 0; j < gHeight; j++)
                {
                   //   textureData[(gWidth * j + i) * 3] = (float)Math.Sqrt(grid.u[i, j] * grid.u[i,j] + grid.v[i, j] * grid.v[i, j]);
                    //  textureData[(gWidth * j + i) * 3 + 1] = grid.e[i,j] / 50f;
                     // textureData[(gWidth * j + i) * 3 + 2] = grid.d[i, j] / 2.5f;

                    textureData[(gWidth * j + i) * 3] = grid.S[i, j];
                    textureData[(gWidth * j + i) * 3 + 1] = 0.2f;
                    textureData[(gWidth * j + i) * 3 + 2] = 1f - grid.S[i,j];
                }
            }
        }
        //GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb32f, gWidth, gHeight, 0, PixelFormat.Rgb, PixelType.Float, textureData);
        
    }
}
