using Avalonia.Platform;
using System;

namespace ALTechUK.WindowingVNCForAvalonia;

internal class HeadlessVncWindowingPlatform(PixelFormat frameBufferFormat, WindowingHeadlessVncConnectionManager connectionManager) 
	: IWindowingPlatform
{
	public IWindowImpl CreateWindow() => new WindowingHeadlessVncWindowImpl(false, frameBufferFormat, connectionManager);

	public IWindowImpl CreateEmbeddableWindow() => throw new PlatformNotSupportedException();

	public IPopupImpl CreatePopup() => new WindowingHeadlessVncWindowImpl(true, frameBufferFormat, connectionManager);

	public ITrayIconImpl? CreateTrayIcon() => null;

	public ITopLevelImpl CreateEmbeddableTopLevel() => throw new NotImplementedException();
}
