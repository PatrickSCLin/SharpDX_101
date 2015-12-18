using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using System.Diagnostics;
using Windows.UI;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;

using Device = SharpDX.Direct3D11.Device;
using Device1 = SharpDX.Direct3D11.Device1;
using Device2 = SharpDX.DXGI.Device2;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SharpDX_YUV
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        byte[] source;

        Device1 device;

        DeviceContext1 context;

        SwapChain2 swapChain;

        Texture2D backTexture;

        RenderTargetView backView;

        public MainPage()
        {
            this.InitializeComponent();

            this.source = File.ReadAllBytes("output.yuv");
        }

        private void SwapChainPanel_Loaded(object sender, RoutedEventArgs e)
        {
            var swapChainPanel = sender as SwapChainPanel;

            this.initDX(swapChainPanel);
        }

        private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.swapChain == null) { return; }

            Size2 newSize = this.RenderSizeToPixelSize(e.NewSize);

            if (newSize.Width > swapChain.Description1.Width || newSize.Height > swapChain.Description1.Height)
            {
                Utilities.Dispose(ref this.backView);

                Utilities.Dispose(ref this.backTexture);

                swapChain.ResizeBuffers(swapChain.Description.BufferCount, (int)e.NewSize.Width, (int)e.NewSize.Height, swapChain.Description1.Format, swapChain.Description1.Flags);

                this.backTexture = Texture2D.FromSwapChain<Texture2D>(this.swapChain, 0);

                this.backView = new RenderTargetView(this.device, this.backTexture);
            }

            swapChain.SourceSize = newSize;
        }

        private void initDX(SwapChainPanel swapChainPanel)
        {
            using (Device d3d11Device = new Device(DriverType.Hardware, DeviceCreationFlags.Debug))
            {
                this.device = d3d11Device.QueryInterface<Device1>();

                this.context = this.device.ImmediateContext1;

                SwapChainDescription1 desc = new SwapChainDescription1()
                {
                    AlphaMode = AlphaMode.Ignore,
                    BufferCount = 2,
                    Flags = SwapChainFlags.None,
                    Format = Format.B8G8R8A8_UNorm,
                    Width = (int)swapChainPanel.ActualWidth,
                    Height = (int)swapChainPanel.ActualHeight,
                    SampleDescription = new SampleDescription(1, 0),
                    Scaling = Scaling.Stretch,
                    Stereo = false,
                    SwapEffect = SwapEffect.FlipSequential,
                    Usage = Usage.BackBuffer | Usage.RenderTargetOutput,
                };

                using (Device2 dxgiDevice = this.device.QueryInterface<Device2>())
                using (Factory2 dxgiFactory = dxgiDevice.Adapter.GetParent<Factory2>())
                using (ISwapChainPanelNative nativePanel = ComObject.As<ISwapChainPanelNative>(swapChainPanel))
                using (SwapChain1 swapChain1 = new SwapChain1(dxgiFactory, this.device, ref desc))
                {
                    this.swapChain = swapChain1.QueryInterface<SwapChain2>();

                    nativePanel.SwapChain = this.swapChain;

                    this.backTexture = Texture2D1.FromSwapChain<Texture2D>(this.swapChain, 0);

                    this.backView = new RenderTargetView(this.device, this.backTexture);

                    CompositionTarget.Rendering += this.draw;
                }
            };
        }

        private void draw(object sender, object e)
        {
            this.context.OutputMerger.SetRenderTargets(this.backView);

            this.context.ClearRenderTargetView(this.backView, new RawColor4(0, 0, 0, 1));
            
            this.swapChain.Present(1, PresentFlags.None, new PresentParameters());
        }

        private Size2 RenderSizeToPixelSize(Size renderSize)
        {
            float pixelScale = Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi / 96.0f;

            return new Size2((int)(renderSize.Width * pixelScale), (int)(renderSize.Height * pixelScale));
        }
    }
}
