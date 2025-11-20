using ALTechUK.WindowingVNCForAvalonia;
using ALTechUK.WindowingVNCForAvalonia.FromAvaloniaSource;
using Avalonia.Controls;
using Avalonia.Platform;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Avalonia;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class WindowingHeadlessVncPlatformExtensions
{
	/// <summary>
	/// Run a headless VNC session where the client size is determined by the size of the 
	/// <seealso cref="Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime.MainWindow"/> 
	/// which supports opening child windows
	/// </summary>
	/// <param name="host">VNC Server IP will be bind, if null or empty <see cref="System.Net.IPAddress.Loopback"/> will be used.</param>
	/// <param name="port">VNC Server port will be bind</param>
	/// <param name="password">VNC connection auth password</param>
	/// <param name="args">Avalonia application start args</param>
	/// <param name="shutdownMode">shut down mode <see cref="ShutdownMode"/></param>
	/// <param name="frameBufferFormat">
	/// The pixel format to send from Skia to the client. 
	/// Defaults to <seealso cref="PixelFormat.Bgra8888"/>
	/// </param>
	/// <param name="framebufferName">The framebuffer name. Many VNC clients set their titlebar to this name.</param>
	public static int StartWithWindowingHeadlessVncPlatform(
		this AppBuilder builder,
		string? host, 
		int port,
		string? password,
		string[] args, ShutdownMode shutdownMode = ShutdownMode.OnLastWindowClose,
		PixelFormat? frameBufferFormat = null,
		string framebufferName = "ALTech UK")
	{
		WindowingHeadlessVncConnectionManager connManager = new(builder, host, port, password, shutdownMode, framebufferName);

		frameBufferFormat ??= PixelFormat.Bgra8888;
		return builder
			.UseHeadlessEx(new AvaloniaHeadlessPlatformOptions
			{
				UseHeadlessDrawing = false,
				FrameBufferFormat = frameBufferFormat.Value
			})
			.AfterPlatformServicesSetup(_ =>
			{
				AvaloniaLocator.CurrentMutable
					.Bind<IWindowingPlatform>().ToConstant(new HeadlessVncWindowingPlatform(
						frameBufferFormat.Value, connManager));
			})
			.StartWithClassicDesktopLifetime(args, shutdownMode);
	}

	//taken from AvaloniaHeadlessPlatformExtensions and modded
	private static AppBuilder UseHeadlessEx(this AppBuilder builder, AvaloniaHeadlessPlatformOptions opts)
	{
		if (opts.UseHeadlessDrawing)
			return builder.UseHeadless(opts);

		return builder
			.UseStandardRuntimePlatformSubsystem()
			.UseWindowingSubsystem(() => {
				AvaloniaHeadlessPlatform.Initialize(opts);
			}, "Headless");
	}
}
