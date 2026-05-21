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
    struct CoordIndexPair
    {
        public int i;
        public int j;
        public int index;
        public CoordIndexPair(int i, int j, int index)
        {
            this.i = i;
            this.j = j;
            this.index = index;
        }

        public override string ToString()
        {
            return "Index: " + index + "  i: " + i + "  j:" + j;
        }
    }
    struct DebugThing
    {
        public int f2d;

        public DebugThing(int f)
        {
            f2d = f;
        }

    }

    struct ShaderSimInfo
    {
        public float dx;
        public float dy;
        public float dt;
        public int mousePosX;
        public int mousePosY;
        public int screenX;
        public int screenY;
        public ShaderSimInfo(float dx, float dy, float dt, Vector2 mousePos, int screnX, int screnY)
        {
            this.dx = dx;
            this.dy = dy;
            this.dt = dt;
            mousePosX = (int)mousePos.X;
            mousePosY = (int)mousePos.Y;
            screenX = screnX;
            screenY = screnY;
        }
    }

    struct ShaderSimInfo2
    {
        public float dx;
        public float dy;
        public float dt;
        public float d;
        public ShaderSimInfo2(float dx, float dy, float dt, float d)
        {
            this.dx = dx;
            this.dy = dy;
            this.dt = dt;
            this.d = d;
        }
    }

    struct DataGroup4
    {
        float R;
        float L;
        float U;
        float D;

        public DataGroup4(float r, float l, float u, float d)
        {
            R = r;
            L = l;
            U = u;
            D = d;
        }
    }

    struct IncompOutData
    {
        public float u;
        public float v;
        public float a_x;
        public float a_y;
        public float a_c;

        public IncompOutData(float u, float v, float ax, float ay, float ac)
        {
            this.u = u;
            this.v = v;
            a_x = ax;
            a_y = ay;
            a_c = ac;
        }
    }

    enum SimState
    {
        Run,
        Paused,
        Step
    }


    enum Processor
    {
        CPU,
        GPU
    }

    enum Solver
    {
        Comp,
        CompRS,
        Incomp
    }

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
    ComputeShader compFHShader;
    ComputeShader compFVShader;
    ComputeShader incompShader;
    int textureHandle;
    int compTextureHandle;
    ShaderSimInfo ssInfo;
    //incompressible
    ShaderSimInfo2 ssInfo2;
    Field2D[,] compShaderDataIn;
    Field2D[,] compShaderDataOut;
    DebugThing[,] compShaderDebugData;
    IncompField2D[,] incompField2D;
    IncompOutData[,] incompOutData;
    IncompField2D[,] incompOutFields;
    //byte array as its currently only a mask
    int[,] compShaderMeshData;
    Field2D sigmaa;
    int ssbo;
    int ssbo1;
    int ssbo2;
    int ssbo3;
    int ssbo4;
    int ssbo5;
    int ssboFH;
    int ssboFV;
    int ssboDebug;
    float[] textureData;
    Grid grid;
    int gWidth;
    int gHeight;
    int zuh;
    int stepTicker;
    SimState simState;
    Processor proc;
    bool debugSSBOEnabled = false;
    bool updateMesh = true;
    OpenTK.Mathematics.Vector2 prevMousePos;
    Solver solver;

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
        gWidth = 600;
        gHeight = (int)(gWidth*0.6f);
        grid = new Grid(gWidth, gHeight);
        compShaderDataIn = new Field2D[gWidth, gHeight];
        compShaderDataOut = new Field2D[gWidth, gHeight];
        incompField2D = new IncompField2D[gWidth, gHeight];
        incompOutData = new IncompOutData[gWidth, gHeight];
        incompOutFields = new IncompField2D[gWidth, gHeight];
        compShaderMeshData = new int[gWidth, gHeight];
        if (debugSSBOEnabled)
        {
            compShaderDebugData = new DebugThing[gWidth, gHeight];
        }
        sigmaa = new Field2D(0.1f, 0.2f, 0.3f, 0.4f, 0.5f);
        grid.StoreGrid(compShaderDataIn, compShaderMeshData);
        grid.StoreGrid(incompField2D, compShaderMeshData);
        simState = SimState.Paused;
        proc = Processor.GPU;
        solver = Solver.Incomp;
        zuh = 0;

        //shader sim parameters
        ssInfo = new ShaderSimInfo(0.15f*(2f/5f),0.15f * (2f / 5f), 0.00075f, Vector2.Zero, width, height);
        ssInfo2 = new ShaderSimInfo2(ssInfo.dx, ssInfo.dy, ssInfo.dt, 1.293f);

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
        if (solver == Solver.CompRS)
        {
            compFHShader = new ComputeShader("Shaders/computeReconstructRiemannH.glsl");
            compFVShader = new ComputeShader("Shaders/computeReconstructRiemannV.glsl");
            computeShader = new ComputeShader("Shaders/computeStep.glsl");
        } else if (solver == Solver.Comp)
        {
            computeShader = new ComputeShader("Shaders/compute.glsl");
        } else
        {
            incompShader = new ComputeShader("Shaders/computeIncompFDM.glsl");
            computeShader = new ComputeShader("Shaders/computeIncompStepFDM.glsl");
        }
        textureHandle = GL.GenTexture();
        compTextureHandle = GL.GenTexture();
        GL.CreateBuffers(1, out ssbo);
        GL.CreateBuffers(1, out ssbo1);
        GL.CreateBuffers(1, out ssbo2);
        GL.CreateBuffers(1, out ssbo3);
        GL.CreateBuffers(1, out ssbo4);
        GL.CreateBuffers(1, out ssbo5);
        if (solver == Solver.CompRS)
        {
            GL.CreateBuffers(1, out ssboFH);
            GL.CreateBuffers(1, out ssboFV);
        }
        if (debugSSBOEnabled)
        {
            GL.CreateBuffers(1, out ssboDebug);
        }
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo);
        if (solver == Solver.Comp || solver == Solver.CompRS)
        {
            unsafe
            {
                GL.BufferData(BufferTarget.ShaderStorageBuffer, compShaderDataIn.Length * sizeof(Field2D) + sizeof(ShaderSimInfo), IntPtr.Zero, BufferUsageHint.DynamicCopy);
                GL.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, sizeof(ShaderSimInfo), ref ssInfo);
                fixed (Field2D* ptr = &compShaderDataIn[0, 0])
                {
                    GL.BufferSubData(BufferTarget.ShaderStorageBuffer, (IntPtr)sizeof(ShaderSimInfo), compShaderDataIn.Length * sizeof(Field2D), (IntPtr)ptr);
                }
            }
        } else
        {
            unsafe
            {
                GL.BufferData(BufferTarget.ShaderStorageBuffer, compShaderDataIn.Length * sizeof(IncompField2D) + sizeof(ShaderSimInfo2), IntPtr.Zero, BufferUsageHint.DynamicCopy);
                GL.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, sizeof(ShaderSimInfo2), ref ssInfo2);
                fixed (IncompField2D* ptr = &incompField2D[0,0])
                {
                    GL.BufferSubData(BufferTarget.ShaderStorageBuffer, (IntPtr)sizeof(ShaderSimInfo2), incompField2D.Length * sizeof(IncompField2D), (IntPtr)ptr);
                }
            }
        }
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, ssbo);
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);


        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo1);
        unsafe
        {
            if (solver == Solver.Comp || solver == Solver.CompRS)
            {
                fixed (Field2D* ptr = &compShaderDataOut[0, 0])
                {
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, compShaderDataOut.Length * sizeof(Field2D), (IntPtr)ptr, BufferUsageHint.DynamicRead);
                }
            } else
            {
                fixed (IncompOutData* ptr = &incompOutData[0,0])
                {
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, incompOutData.Length * sizeof(IncompOutData), (IntPtr)ptr, BufferUsageHint.DynamicCopy);
                }
            }

        }
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, ssbo1);
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo2);
        unsafe
        {
            fixed (int* ptr2 = &compShaderMeshData[0,0])
            {
                GL.BufferData(BufferTarget.ShaderStorageBuffer, compShaderMeshData.Length * sizeof(int), (IntPtr)ptr2, BufferUsageHint.DynamicRead);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 4, ssbo2);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            }
        }

        if (solver == Solver.Comp || solver == Solver.CompRS)
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo3);
            unsafe
            {
                fixed (Field2D* ptr3 = &compShaderDataIn[0, 0])
                {
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, compShaderDataOut.Length * sizeof(Field2D), (IntPtr)ptr3, BufferUsageHint.DynamicRead);
                }
            }
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 6, ssbo3);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }
        
        if (solver == Solver.Incomp)
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo4);
            unsafe
            {
                fixed (IncompField2D* ptr4 = &incompOutFields[0,0])
                {
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, incompOutFields.Length * sizeof(IncompField2D), (IntPtr)ptr4, BufferUsageHint.DynamicRead);
                }
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 8, ssbo4);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            }
        }

        if (solver == Solver.Incomp)
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo5);
            unsafe
            {
                GL.BufferData(BufferTarget.ShaderStorageBuffer, gWidth * gHeight * sizeof(double), IntPtr.Zero, BufferUsageHint.DynamicRead);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 7, ssbo5);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            }
        }

        if (solver == Solver.CompRS)
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssboFH);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(Field2D) * gWidth * (gHeight + 1), IntPtr.Zero, BufferUsageHint.DynamicRead);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 7, ssboFH);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);


            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssboFV);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(Field2D) * (gWidth + 1) * gHeight, IntPtr.Zero, BufferUsageHint.DynamicRead);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 8, ssboFV);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }

        if (debugSSBOEnabled)
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssboDebug);
            unsafe
            {
                fixed (DebugThing* ptr = &compShaderDebugData[0, 0])
                {
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, compShaderDebugData.Length * sizeof(DebugThing), (IntPtr)ptr, BufferUsageHint.DynamicRead);
                }
            }
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 5, ssboDebug);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }
        
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
        computeShader.Dispose();
        if (solver == Solver.CompRS)
        {
            compFHShader.Dispose();
            compFVShader.Dispose();
        } else if (solver == Solver.Incomp)
        {
            incompShader.Dispose();
        }

    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        Title = "Sigma" + (int)(MousePosition.X * ((float)gWidth / ClientSize.X)) + ", " + (int)(MousePosition.Y * ((float)gHeight / ClientSize.Y));
        GL.Clear(ClearBufferMask.ColorBufferBit);
        if (prevMousePos != MousePosition)
        {
            if (solver == Solver.Comp || solver == Solver.CompRS)
            {
                Field2D mouseCell = compShaderDataOut[Math.Clamp((int)(MousePosition.X * ((float)gWidth / ClientSize.X)), 0, gWidth - 1), Math.Clamp(gHeight - (int)(MousePosition.Y * ((float)gHeight / ClientSize.Y)), 0, gHeight - 1)];
                Console.WriteLine("d:" + mouseCell.d + " u:" + mouseCell.u + " v:" + mouseCell.v + " E:" + mouseCell.E);
            }
            prevMousePos = MousePosition;
        }
        if (proc == Processor.GPU && simState != SimState.Paused)
        {
            zuh++;
            Console.WriteLine("step:" + zuh + " t:" + (zuh * ssInfo.dt).ToString("0.000000") + "                fps:" + (1 / e.Time).ToString("#.#"));
            if (simState == SimState.Step)
            {
                stepTicker--;
                if (stepTicker == 0)
                {
                    simState = SimState.Paused;
                }
            }
            if (solver == Solver.Comp || solver == Solver.CompRS)
            {
                ssInfo.mousePosX = (int)MousePosition.X;
                ssInfo.mousePosY = (int)MousePosition.Y;
            }

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo);
            unsafe
            {
                if (solver == Solver.CompRS || solver == Solver.Comp)
                {
                    fixed (void* dataPtr = &compShaderDataIn[0, 0])
                    {
                        IntPtr ptr = GL.MapBufferRange(BufferTarget.ShaderStorageBuffer, (IntPtr)(sizeof(ShaderSimInfo)), compShaderDataIn.Length * sizeof(Field2D), MapBufferAccessMask.MapWriteBit);
                        System.Buffer.MemoryCopy(dataPtr, ptr.ToPointer(), compShaderDataIn.Length * sizeof(Field2D), compShaderDataIn.Length * sizeof(Field2D));
                        GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
                    }
                    fixed (ShaderSimInfo* ssInfoPtr = &ssInfo)
                    {
                        IntPtr ptrH = GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.WriteOnly);
                        System.Buffer.MemoryCopy(ssInfoPtr, ptrH.ToPointer(), sizeof(ShaderSimInfo), sizeof(ShaderSimInfo));
                        GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
                    }
                } else
                {
                    fixed (void* dataPtr = &incompField2D[0,0])
                    {
                        IntPtr ptr = GL.MapBufferRange(BufferTarget.ShaderStorageBuffer, (IntPtr)(sizeof(ShaderSimInfo2)), incompField2D.Length * sizeof(IncompField2D), MapBufferAccessMask.MapWriteBit);
                        System.Buffer.MemoryCopy(dataPtr, ptr.ToPointer(), incompField2D.Length * sizeof(IncompField2D), incompField2D.Length * sizeof(IncompField2D));
                        GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
                    }
                    fixed (ShaderSimInfo2* ssInfoPtr = &ssInfo2)
                    {
                        IntPtr ptrH = GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.WriteOnly);
                        System.Buffer.MemoryCopy(ssInfoPtr, ptrH.ToPointer(), sizeof(ShaderSimInfo2), sizeof(ShaderSimInfo2));
                        GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
                    }
                }
            }
            if (solver == Solver.CompRS)
            {
                compFVShader.Use();
                GL.DispatchCompute(gWidth + 1, gHeight, 1);
                compFHShader.Use();
                GL.DispatchCompute(gWidth, gHeight + 1, 1);
            }
            if (solver == Solver.Incomp)
            {
                incompShader.Use();
                GL.DispatchCompute(gWidth, gHeight, 1);
                GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);

                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo1);
                IntPtr ptr333 = GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.ReadWrite);
                unsafe
                {
                    fixed (void* dataPtr123 = &incompOutData[0,0])
                    {
                        System.Buffer.MemoryCopy(ptr333.ToPointer(), dataPtr123, incompOutData.Length * sizeof(IncompOutData), incompOutData.Length * sizeof(IncompOutData));
                    }
                    GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
                }
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                alglib.sparsematrix A = new();
                alglib.xparams xparams = new(0);
                alglib.sparsecreatecrsempty(gWidth*gHeight, out A, xparams);
                double[] b = new double[gWidth*gHeight];
                for (int i = 0; i < gWidth; i++)
                {
                    for (int j = 0; j < gHeight; j++)
                    {
                        IncompOutData data = incompOutData[i, j];
                        double[] vals;
                        int[] indices;
                        int centerIndex = j + i * gHeight;

                        if (i == 0)
                        {
                            if (j == 0)
                            {
                                //c,n,e
                                indices = new int[3] { centerIndex, centerIndex + 1, centerIndex + gHeight };
                                vals = new double[3] { data.a_c + data.a_x + data.a_y, data.a_y, data.a_x};
                                b[j + i * gHeight] = 0;
                            } else if  (j == gHeight-1)
                            {
                                //s,c,e
                                indices = new int[3] { centerIndex - 1, centerIndex, centerIndex + gHeight };
                                vals = new double[3] { data.a_y, data.a_c + data.a_x + data.a_y, data.a_x };
                                b[j + i * gHeight] = 0;
                            } else
                            {
                                //s,c,n,e
                                indices = new int[4] {centerIndex - 1, centerIndex, centerIndex + 1, centerIndex + gHeight };
                                vals = new double[4] { data.a_y, data.a_c + data.a_x, data.a_y, data.a_x };
                                b[j + i * gHeight] = (incompOutData[i, j + 1].v - incompOutData[i, j - 1].v) / (2f * ssInfo2.dy); //-a_x * 0 * dx
                            }
                        } else if (i == gWidth-1)
                        {
                            if (j == 0)
                            {
                                //w,c,n
                                indices = new int[3] { centerIndex - gHeight, centerIndex, centerIndex + 1 };
                                vals = new double[3] { data.a_x, data.a_c + data.a_x + data.a_y, data.a_y };
                                b[j + i * gHeight] = 0;
                            }
                            else if (j == gHeight - 1)
                            {
                                //w,s,c
                                indices = new int[3] { centerIndex - gHeight, centerIndex - 1, centerIndex };
                                vals = new double[3] { data.a_x, data.a_y, data.a_c + data.a_x + data.a_y };
                                b[j + i * gHeight] = 0;
                            }
                            else
                            {
                                //w,s,c,n
                                indices = new int[4] { centerIndex - gHeight, centerIndex - 1, centerIndex, centerIndex + 1};
                                vals = new double[4] { data.a_x, data.a_y, data.a_c + data.a_x, data.a_y };
                                b[j + i * gHeight] = (incompOutData[i, j + 1].v - incompOutData[i, j - 1].v) / (2f * ssInfo2.dy); //-a_x * 0 * dx
                            }
                        } else
                        {
                            if (j == 0)
                            {
                                //w,c,n,e
                                indices = new int[4] { centerIndex - gHeight, centerIndex, centerIndex+1, centerIndex + gHeight };
                                vals = new double[4] { data.a_x, data.a_c + data.a_y, data.a_y, data.a_x };
                                b[j + i * gHeight] = (incompOutData[i + 1, j].u - incompOutData[i - 1, j].u) / (2f * ssInfo2.dx); //+a_y * 0 * dy
                            }
                            else if (j == gHeight - 1)
                            {
                                //w,s,c,e
                                indices = new int[4] { centerIndex - gHeight, centerIndex - 1, centerIndex, centerIndex + gHeight };
                                vals = new double[4] { data.a_x, data.a_y, data.a_c + data.a_y, data.a_x };
                                b[j + i * gHeight] = (incompOutData[i + 1, j].u - incompOutData[i - 1, j].u) / (2f * ssInfo2.dx); //-a_y * 0 * dy
                            }
                            else
                            {
                                //w,s,c,n,e
                                indices = new int[5] { centerIndex - gHeight, centerIndex - 1, centerIndex, centerIndex + 1, centerIndex + gHeight };
                                vals = new double[5] { data.a_x, data.a_y, data.a_c, data.a_y, data.a_x };
                                b[j + i * gHeight] = (incompOutData[i + 1, j].u - incompOutData[i-1,j].u)/(2f*ssInfo2.dx) + (incompOutData[i, j + 1].v - incompOutData[i,j-1].v)/(2f*ssInfo2.dy);
                            }
                        }
                        
                        alglib.sparseappendcompressedrow(A, indices, vals, indices.Length, new alglib.xparams(0));
                    }
                }
                //alglib.sparsesolve(A, b, 0, out double[] solvedP, out alglib.sparsesolverreport report);
                alglib.sparsesolvegmres(A, b, 200, 0.01f, 1000, out double[] solvedP, out alglib.sparsesolverreport report);
                Console.WriteLine("itr:" + report.iterationscount + " r2:" + report.r2 + " t:" + report.terminationtype + " nmv:" + report.nmv);
                Console.WriteLine(incompOutData[200, 200].a_c);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo5);
                IntPtr mapPTR = GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.WriteOnly);
                unsafe
                {
                    fixed (double* ptr7 = &solvedP[0])
                    {
                        System.Buffer.MemoryCopy(ptr7, mapPTR.ToPointer(), solvedP.Length * sizeof(double), solvedP.Length * sizeof(double));
                    }
                }
                GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);

            }
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
            computeShader.Use();
            GL.DispatchCompute(gWidth, gHeight, 1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);

            unsafe
            {
                if (solver == Solver.Comp || solver == Solver.CompRS)
                {
                    fixed (void* dataPtr = &compShaderDataOut[0, 0])
                    {
                        fixed (void* dataPtr2 = &compShaderDataIn[0, 0])
                        {
                            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo1);
                            IntPtr ptr1 = GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.ReadWrite);
                            System.Buffer.MemoryCopy(ptr1.ToPointer(), dataPtr, compShaderDataOut.Length * sizeof(Field2D), compShaderDataOut.Length * sizeof(Field2D));
                            GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);

                            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo3);
                            IntPtr ptr5 = GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.WriteOnly);
                            System.Buffer.MemoryCopy(dataPtr2, ptr5.ToPointer(), compShaderDataOut.Length * sizeof(Field2D), compShaderDataOut.Length * sizeof(Field2D));
                            GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
                            System.Buffer.MemoryCopy(dataPtr, dataPtr2, compShaderDataOut.Length * sizeof(Field2D), compShaderDataOut.Length * sizeof(Field2D));
                        }
                    }
                }
                else
                {
                    fixed (void* ptrr = &incompOutFields[0,0]) 
                    {
                        fixed (void* ptrrr = &incompField2D[0,0])
                        {
                            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo4);
                            IntPtr ptr1 = GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.ReadWrite);
                            System.Buffer.MemoryCopy(ptr1.ToPointer(), ptrr, incompOutFields.Length * sizeof(IncompField2D), incompOutFields.Length * sizeof(IncompField2D));
                            System.Buffer.MemoryCopy(ptrr, ptrrr, incompOutFields.Length * sizeof(IncompField2D), incompOutFields.Length * sizeof(IncompField2D));
                            GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
                        }
                    }
                }
            }

            if (updateMesh)
            {
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo2);
                unsafe
                {
                    fixed (int* ptr3 = &compShaderMeshData[0, 0])
                    {
                        IntPtr ptrH = GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.WriteOnly);
                        System.Buffer.MemoryCopy(ptr3, ptrH.ToPointer(), compShaderMeshData.Length * sizeof(int), compShaderMeshData.Length * sizeof(int));
                        GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
                    }
                }
                updateMesh = false;
            }

            if (debugSSBOEnabled)
            {
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssboDebug);
                IntPtr ptr2 = GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.ReadWrite);
                unsafe
                {
                    fixed (void* debugDataPtr = &compShaderDebugData[0, 0])
                    {
                        System.Buffer.MemoryCopy(ptr2.ToPointer(), debugDataPtr, compShaderDebugData.Length * sizeof(DebugThing), compShaderDebugData.Length * sizeof(DebugThing));
                    }
                }
                GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
            }
        }
        
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
        if (proc == Processor.CPU)
        {
            grid.TimeStep(0.0006f);
            for (int i = 0; i < gWidth; i++)
            {
                for (int j = 0; j < gHeight; j++)
                {
                    textureData[(gWidth * j + i) * 3] = (float)Math.Sqrt(grid.u[i, j] * grid.u[i, j] + grid.v[i, j] * grid.v[i, j]);
                    textureData[(gWidth * j + i) * 3 + 1] = grid.e[i, j] / 50f;
                    textureData[(gWidth * j + i) * 3 + 2] = grid.d[i, j] / 2.5f;

                    textureData[(gWidth * j + i) * 3] = grid.S[i, j];
                    textureData[(gWidth * j + i) * 3 + 1] = 0.2f;
                    textureData[(gWidth * j + i) * 3 + 2] = 1f - grid.S[i, j];
                }
            }
            // GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb32f, gWidth, gHeight, 0, PixelFormat.Rgb, PixelType.Float, textureData);
        }
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.Key)
        {
            case Keys.P:
                if (simState == SimState.Paused)
                {
                    simState = SimState.Run;
                } else
                {
                    simState = SimState.Paused;
                }
                break;
            case Keys.U:
                simState = SimState.Step;
                stepTicker = 1;
                break;
            case Keys.I:
                simState = SimState.Step;
                stepTicker = 10;
                break;
            case Keys.O:
                simState = SimState.Step;
                stepTicker = 50;
                break;
        }
    }
}
