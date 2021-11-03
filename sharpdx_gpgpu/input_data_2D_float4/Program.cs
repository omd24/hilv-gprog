/// <summary>
/// demo showing working with 2D input texture with float4 elements
/// i.e., each element on the 2D texture is a float4
/// </summary>

using System;
using System.Drawing;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;

namespace input_data_2D_float4
{
    class Program
    {
        public void Run()
        {
            // -- init device and swapchain:
            var device = new Device(DriverType.Hardware, DeviceCreationFlags.None);
            var context = device.ImmediateContext;
            const int Width = 32;
            // N.B., try to avoid alignment issues...
            if (Width % 32 != 0)
                throw new ArgumentException("Not a valid width value");
            const int Height = 2;
            const int Width_DataStream = Width * 4 /* four floats on DataStream represent each element on the texture */;
            Console.WriteLine("Texture Size: ({0},{1}) - Count: {2}", Width, Height, Width * Height);
            Console.WriteLine();

            // -- create input buffer
            var input_data = new DataStream(sizeof(float) * Width_DataStream * Height, true, true);
            int k = 0;
            for (int i = 0; i < Width_DataStream * Height;)
            {
                input_data.Write<float>(k);     // x
                input_data.Write<float>(k);     // y
                input_data.Write<float>(-k);    // z
                input_data.Write<float>(-k);    // w
                i += 4;
                ++k;
            }
            // -- check data integrity
            unsafe
            {
                var buffer = (float*)input_data.DataPointer;
                Console.WriteLine("Input Data:");
                for (int i = 0; i < Width_DataStream * Height /*10*/ /*show only first 10 obj*/;)
                {
                    Console.WriteLine("({0}, {1,2})", buffer[i] + buffer[i + 1], buffer[i + 2] + buffer[i + 3]);
                    i += 4;
                }
            }
            Console.WriteLine();
            // Create input texture (convert DataStream to an InputTexture)
            var texture = new Texture2D(
                device,
                new Texture2DDescription
                {
                    ArraySize = 1,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Format = Format.R32G32B32A32_Float,
                    Width = Width,
                    Height = Height,
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.None,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Immutable
                }, new DataRectangle(input_data.DataPointer, sizeof(float) * Width_DataStream));
            var input_tex_view = new ShaderResourceView(device, texture);

            Console.WriteLine("Compiling Shaders...");
            Console.WriteLine();

            var demo = new GpGpuApp();
            demo.Size = new Size(Width, Height);
            demo.Initialize(device);

            Console.WriteLine("Running Tests...");
            Console.WriteLine();
            demo.DoDraw(context, input_tex_view);

            context.Flush();
            demo.GetResults(context);

            Console.WriteLine();
        }
        static void Main(string[] args)
        {
            var program = new Program();
            program.Run();
            Console.ReadKey();
        }
    }
}
