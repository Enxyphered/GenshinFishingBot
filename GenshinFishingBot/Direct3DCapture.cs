using System;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using Resource = SharpDX.DXGI.Resource;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace GenshinFishingBot
{
    public class Direct3DCapture
    {
        private Device _device;
        private Adapter1 _adapter;
        private Texture2DDescription _textureDescription;
        private OutputDescription _outputDescription;
        private OutputDuplication _output;
        private Output1 _output1;
        private Texture2D _outputImg = null;

        public int Width
        {
            get => _outputDescription.DesktopBounds.Right;
        }

        public int Height
        {
            get => _outputDescription.DesktopBounds.Bottom;
        }

        public Direct3DCapture()
            : this(0, 0) { }

        public Direct3DCapture(int whichMonitor)
            : this(0, whichMonitor) { }

        public Direct3DCapture(int whichGraphicsCardAdapter, int whichMonitor)
        {
            _adapter = new Factory1().GetAdapter1(whichGraphicsCardAdapter);
            _device = new Device(_adapter);
            Output output = _adapter.GetOutput(whichMonitor);
            _output1 = output.QueryInterface<Output1>();
            _outputDescription = output.Description;
            _textureDescription = new Texture2DDescription()
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = Width,
                Height = Height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };

            _output = _output1.DuplicateOutput(_device);
        }

        public Span<byte> NextFrame()
        {
            Span<byte> bufferOutput;

            if (_outputImg == null)
                _outputImg = new Texture2D(_device, _textureDescription);

            Resource desktopResource = null;
            OutputDuplicateFrameInformation frameInfo;

            Result result = Result.Fail;
            while (result.Failure)
            {
                result = _output.TryAcquireNextFrame(0, out frameInfo, out desktopResource);
            }

            using (var gpuOutput = desktopResource.QueryInterface<Texture2D>())
                _device.ImmediateContext.CopyResource(gpuOutput, _outputImg);

            var mapSource = _device.ImmediateContext.MapSubresource(_outputImg, 0, MapMode.Read, MapFlags.None);
            var len = _textureDescription.Width * 4 * _textureDescription.Height;

            unsafe
            {
                var sourcePtr = (byte*)mapSource.DataPointer;
                bufferOutput = new Span<byte>(sourcePtr, len);
            }

            _device.ImmediateContext.UnmapSubresource(_outputImg, 0); 
            _output.ReleaseFrame();

            return bufferOutput;
        }
    }
}
