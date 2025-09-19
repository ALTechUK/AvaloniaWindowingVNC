using Avalonia.Controls;
using System;
using Avalonia;
using RemoteViewing.Vnc;
using RemoteViewing.Vnc.Server;
using ALTechUK.AvaloniaWindowingVNC.FromAvaloniaSource;

namespace ALTechUK.AvaloniaWindowingVNC;

internal class CustomHeadlessVncFramebufferSource : HeadlessVncFramebufferSource, IVncFramebufferSource
{
#if NET9_0_OR_GREATER
	readonly System.Threading.Lock _lock = new();
#else
	readonly object _lock = new();
#endif
	readonly string _frameBufferName;

	public CustomHeadlessVncFramebufferSource(VncServerSession session, Window window, string framebufferName) 
		: base(session, window)
	{
		Window = window;
		_frameBufferName = string.IsNullOrEmpty(framebufferName) ? "ALTech UK" : framebufferName;
		_framebuffer = new VncFramebuffer(
			_frameBufferName,
			(int)Math.Ceiling(window.ClientSize.Width),
			(int)Math.Ceiling(window.ClientSize.Height),
			VncPixelFormat.RGB32);
	}

	unsafe VncFramebuffer IVncFramebufferSource.Capture()
	{
		lock (_lock)
		{
			using var bmpRef = Window.GetLastRenderedFrame();

			if (bmpRef == null)
				return _framebuffer;
			var bmp = bmpRef;
			if (bmp.PixelSize.Width != _framebuffer.Width || bmp.PixelSize.Height != _framebuffer.Height)
			{
				_framebuffer = new VncFramebuffer(_frameBufferName, bmp.PixelSize.Width, bmp.PixelSize.Height,
					VncPixelFormat.RGB32);
			}

			var buffer = _framebuffer.GetBuffer();
			fixed (byte* bufferPtr = buffer)
			{
				bmp.CopyPixels(new PixelRect(default, bmp.PixelSize), (nint)bufferPtr, buffer.Length, _framebuffer.Stride);
			}
		}

		return _framebuffer;
	}
}
