using ALTechUK.AvaloniaWindowingVNC.FromAvaloniaSource;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging;
using Avalonia.Threading;
using RemoteViewing.Vnc;
using RemoteViewing.Vnc.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ALTechUK.AvaloniaWindowingVNC;

internal class WindowingHeadlessVncConnectionManager
{
	readonly TcpListener _tcpListener;
	readonly ShutdownMode _shutdownMode;
	readonly ConcurrentDictionary<string, CustomHeadlessVncFramebufferSource> _vncFrameBuffers = new();
	readonly Stack<Window> _displayWindows;
	readonly string _framebufferName;
	readonly string? _connPassword;
	readonly AvaloniaVncLogger _vncLogger;

	IClassicDesktopStyleApplicationLifetime? _appLifetime;
	Window? _currentWindow;

	Window DisplayWindow => _currentWindow ?? _appLifetime?.MainWindow
		?? throw new InvalidOperationException("MainWindow wasn't initialized");

	public Size? ClientSize { get; private set; }

	public WindowingHeadlessVncConnectionManager(AppBuilder appBuilder, string? host, int port, string? password, ShutdownMode shutdownMode, string framebufferName)
	{
		_tcpListener = new(host == null ? IPAddress.Loopback : IPAddress.Parse(host), port);
		_shutdownMode = shutdownMode;
		_displayWindows = new();
		_framebufferName = framebufferName;
		_connPassword = password;
		_vncLogger = new AvaloniaVncLogger();

		appBuilder.AfterSetup(_ =>
		{
			_appLifetime = (IClassicDesktopStyleApplicationLifetime)appBuilder.Instance!.ApplicationLifetime!;
			_appLifetime!.Startup += AppStartup;
		});
	}

	private async void AppStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
	{
		//only start listening once the window has initialised so we know it's client size
		while (_appLifetime?.MainWindow == null || !_appLifetime.MainWindow.IsActive)
		{
			await Task.Delay(100);
			Dispatcher.UIThread.RunJobs();
		}
		ClientSize = DisplayWindow.ClientSize;

		char[]? passwordChars = _connPassword?.ToCharArray();
		VncServerSessionOptions sessionOptions = new()
		{
			AuthenticationMethod = passwordChars == null
									? AuthenticationMethod.None
									: AuthenticationMethod.Password
		};
		void paswordHandler(object? s, PasswordProvidedEventArgs e)
		{
			if (passwordChars == null)
				e.Accept();
			else
				e.Accept(passwordChars);
		}

		_tcpListener.Start();
		while (true)
		{
			try
			{
				var client = await _tcpListener.AcceptTcpClientAsync();
				string connectionId = Guid.NewGuid().ToString();
				
				var session = new VncServerSession(new VncPasswordChallenge(), logger: _vncLogger);
				session.PasswordProvided += paswordHandler;

				var frameBuffer = new CustomHeadlessVncFramebufferSource(session, DisplayWindow, _framebufferName);
				_vncFrameBuffers.TryAdd(connectionId, frameBuffer);

				session.SetFramebufferSource(frameBuffer);
				session.Connect(client.GetStream(), sessionOptions);
				session.Closed += (s, e) => SessionClosed(connectionId);
			}
			catch (Exception ex)
			{
				Logger.TryGet(LogEventLevel.Error, LogArea.VncPlatform)?.Log(_tcpListener, "Error accepting client:{Exception}", ex);
			}
			finally
			{
				await Task.Delay(100);
			}
		}
	}

	internal void SetCurrentWindow(Window window)
	{
		if (window == DisplayWindow)
			return;

		_displayWindows.Push(DisplayWindow);
		_currentWindow = window;
		foreach (CustomHeadlessVncFramebufferSource frameBuffer in _vncFrameBuffers.Values)
			frameBuffer.Window = window;
	}

	internal void WindowClosed()
	{
        _displayWindows.TryPop(out _currentWindow);
		foreach (CustomHeadlessVncFramebufferSource frameBuffer in _vncFrameBuffers.Values)
			frameBuffer.Window = DisplayWindow;
	}

	internal void SessionClosed(string sessionId)
	{
		_vncFrameBuffers.TryRemove(sessionId, out _);
		if (_shutdownMode == ShutdownMode.OnLastWindowClose && _vncFrameBuffers.IsEmpty)
			Dispatcher.UIThread.InvokeShutdown();
	}
}
