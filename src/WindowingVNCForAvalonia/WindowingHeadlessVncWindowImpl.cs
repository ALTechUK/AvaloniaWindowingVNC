using ALTechUK.WindowingVNCForAvalonia.FromAvaloniaSource;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using System;

namespace ALTechUK.WindowingVNCForAvalonia;
internal sealed class WindowingHeadlessVncWindowImpl : HeadlessWindowImpl, IDisposable
{
	readonly WindowingHeadlessVncConnectionManager _connectionManager;

	Window? _rootWindowControl;
	bool _closed;

	public WindowingHeadlessVncWindowImpl(bool isPopup, PixelFormat frameBufferFormat, WindowingHeadlessVncConnectionManager connectionManager)
		: base(isPopup, frameBufferFormat)
	{
		_connectionManager = connectionManager;
		if (connectionManager.ClientSize != null)
		{
			ClientSize = connectionManager.ClientSize.Value;
		}

		Closed += OnClose;
	}

	private void OnClose()
	{
		if (_closed)
			return;

		_connectionManager.WindowClosed();
		_closed = true;
	}

	void IDisposable.Dispose()
	{
		OnClose();
		base.Dispose();
	}

	public override void Show(bool activate, bool isDialog)
	{
		base.Show(activate, isDialog);
		if (_rootWindowControl != null)
			_connectionManager.SetCurrentWindow(_rootWindowControl);
	}

	public override void SetInputRoot(IInputRoot inputRoot)
	{
		if (InputRoot is Window oldWindow)
			oldWindow.PropertyChanged -= WindowPropChanged;

		base.SetInputRoot(inputRoot);
		if (inputRoot is Window window)
		{
			_rootWindowControl = window;
			window.SizeToContent = SizeToContent.Manual;
			window.PropertyChanged += WindowPropChanged;
		}
	}

	void WindowPropChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (sender is not Window window)
			return;

		window.SizeToContent = SizeToContent.Manual;
	}
}

