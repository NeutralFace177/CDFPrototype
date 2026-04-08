using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace CFDPrototype
{
    public class ComputeShader
    {
        public int handle;
        private bool disposedValue = false;
        public ComputeShader(string path)
        {
            int shader;

            string shaderSource = File.ReadAllText(path);

            shader = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(shader, shaderSource);

            GL.CompileShader(shader);
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                Console.WriteLine(GL.GetShaderInfoLog(shader));
            }

            handle = GL.CreateProgram();

            GL.AttachShader(handle, shader);

            GL.LinkProgram(handle);

            GL.GetProgram(handle, GetProgramParameterName.LinkStatus, out int success2);
            if (success2 == 0)
            {
                Console.WriteLine(GL.GetProgramInfoLog(handle));
            }

            GL.DetachShader(handle, shader);
            GL.DeleteShader(shader);

        }

        public void Use()
        {
            GL.UseProgram(handle);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                GL.DeleteProgram(handle);
                disposedValue = true;
            }
        }

        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(handle, attribName);
        }

        ~ComputeShader()
        {
            if (disposedValue == false)
            {
                Console.WriteLine("GPU Resource leak! Did you forget to call Dispose()?");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
