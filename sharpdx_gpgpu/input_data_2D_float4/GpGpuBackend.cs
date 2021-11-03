/// <summary>
/// backend code for general purpose gpu programming
/// </summary>

using System;
using System.Drawing;

using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

using Buffer = SharpDX.Direct3D11.Buffer;
using Color = SharpDX.Color;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace input_data_2D_float4
{
    public class GpGpuApp
    {
        private VertexShader vs;

        private PixelShader ps;

        private InputLayout layout;

        private Buffer vertices;

        private SamplerState sampler;

        private Device dev;

        private Size size = Size.Empty;

        private Texture2D render_target;

        private RenderTargetView render_target_view;

        private Texture2D texture_readback;

        public void Initialize(Device device)
        {
            dev = device;

            // -- compile vertex and pixel shaders
            var bytecode = ShaderBytecode.CompileFromFile("default.hlsl", "vs_main", "vs_5_0");
            vs = new VertexShader(device, bytecode);
            // -- layout from vs input signature
            layout = new InputLayout(device, ShaderSignature.GetInputSignature(bytecode), new[] {
                            new InputElement("POSITION", 0, Format.R32G32_Float, 0, 0)
                        });
            bytecode.Dispose();

            bytecode = ShaderBytecode.CompileFromFile("default.hlsl", "ps_main", "ps_5_0");
            ps = new PixelShader(device, bytecode);
            bytecode.Dispose();

            // -- instantiate vertex buffer from vertex data
            vertices = Buffer.Create(device, BindFlags.VertexBuffer, new float[]{
                -1.0f, 1.0f,
                1.0f, 1.0f,
                -1.0f, -1.0f,
                1.0f, -1.0f,
            });

            sampler = new SamplerState(device, new SamplerStateDescription()
            {
                Filter = Filter.MinMagMipPoint,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                BorderColor = Color.Black,
                ComparisonFunction = Comparison.Never,
                MaximumAnisotropy = 16,
                MipLodBias = 0,
                MinimumLod = 0,
                MaximumLod = 16,
            });
            // -- create readback texture
            texture_readback = new Texture2D(
                device,
                new Texture2DDescription
                {
                    ArraySize = 1,
                    BindFlags = BindFlags.None,
                    CpuAccessFlags = CpuAccessFlags.Read,
                    Format = Format.R32G32_Float,
                    Width = size.Width,
                    Height = size.Height,
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.None,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Staging
                });

            UpdateMinMaxTextures();
        }
        public void GetResults(DeviceContext context)
        {
            // -- copy tex resource to a Readback and read data using a DataStream
            context.CopySubresourceRegion(render_target, 0, null, texture_readback, 0, 0, 0, 0);
            DataStream result;
            context.MapSubresource(texture_readback, 0, MapMode.Read, MapFlags.None, out result);
            unsafe
            {
                var buffer = (float*)result.DataPointer;
                int total_data_count = size.Width * size.Height * 2 /*each two float on data stream corresponds to one elem on tex*/;
                Console.WriteLine("Output Data:");
                for (int i = 0; i < total_data_count /*10*/ /*only the first 10 objs*/;)
                {
                    Console.WriteLine("({0}, {1,2})", buffer[i], buffer[i + 1]);
                    i += 2;
                }
            }
            context.UnmapSubresource(texture_readback, 0);
        }

        public Size Size { get { return size; } set { size = value; } }

        private void UpdateMinMaxTextures()
        {
            // -- create render_target
            render_target = new Texture2D(dev, new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R32G32_Float,   // PS return type: x, y
                Width = size.Width,
                Height = size.Height,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            });

            // Create render target view
            render_target_view = new RenderTargetView(dev, render_target, new RenderTargetViewDescription()
            {
                Format = Format.R32G32_Float,   // PS return type: x, y
                Dimension = RenderTargetViewDimension.Texture2D,
                Texture2D = { MipSlice = 0 }
            });

        }
        public void DoDraw(DeviceContext context, ShaderResourceView input_tex_view)
        {
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, sizeof(float) * 2, 0));
            context.VertexShader.Set(vs);

            var viewport = new Viewport(0, 0, Size.Width, Size.Height);

            context.PixelShader.Set(ps);
            context.PixelShader.SetSampler(0, sampler);
            context.PixelShader.SetShaderResource(0, input_tex_view);
            context.Rasterizer.SetViewport(viewport);
            //context.ClearRenderTargetView(render_target_view, Colors.Black);
            context.OutputMerger.SetTargets(render_target_view);
            context.Draw(4, 0);
            context.PixelShader.SetShaderResource(0, null);
            context.OutputMerger.ResetTargets();
        }
    }
}
