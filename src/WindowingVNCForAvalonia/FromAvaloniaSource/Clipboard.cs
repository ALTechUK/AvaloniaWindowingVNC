using Avalonia.Input;
using Avalonia.Input.Platform;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ALTechUK.WindowingVNCForAvalonia.FromAvaloniaSource;

internal sealed class Clipboard(IClipboardImpl clipboardImpl) : IClipboard
{
	private readonly IClipboardImpl _clipboardImpl = clipboardImpl;
	private IAsyncDataTransfer? _lastDataTransfer;

	Task<string?> IClipboard.GetTextAsync()
		=> this.TryGetTextAsync();

	Task IClipboard.SetTextAsync(string? text)
		=> this.SetValueAsync(DataFormat.Text, text);

	public Task ClearAsync()
	{
		_lastDataTransfer?.Dispose();
		_lastDataTransfer = null;

		return _clipboardImpl.ClearAsync();
	}

	[Obsolete($"Use {nameof(SetDataAsync)} instead.")]
	Task IClipboard.SetDataObjectAsync(IDataObject data)
		=> throw new NotSupportedException();

	public Task SetDataAsync(IAsyncDataTransfer? dataTransfer)
	{
		if (dataTransfer is null)
			return ClearAsync();

		if (_clipboardImpl is IOwnedClipboardImpl)
			_lastDataTransfer = dataTransfer;

		return _clipboardImpl.SetDataAsync(dataTransfer);
	}

	public Task FlushAsync()
		=> _clipboardImpl is IFlushableClipboardImpl flushable ? flushable.FlushAsync() : Task.CompletedTask;

	async Task<string[]> IClipboard.GetFormatsAsync()
	{
		var dataTransfer = await TryGetDataAsync();
		return dataTransfer is null ? [] : dataTransfer.Formats.Select(f =>
		{
			if (DataFormats.Text.Equals(f))
				return DataFormats.Text;

			if (DataFormats.Files.Equals(f))
				return DataFormats.Files;

			return f.Identifier;
		}).ToArray();
	}

	[Obsolete($"Use {nameof(TryGetDataAsync)} instead.")]
	async Task<object?> IClipboard.GetDataAsync(string format) => throw new NotSupportedException();

	public Task<IAsyncDataTransfer?> TryGetDataAsync()
		=> _clipboardImpl.TryGetDataAsync();

	[Obsolete($"Use {nameof(TryGetInProcessDataAsync)} instead.")]
	async Task<IDataObject?> IClipboard.TryGetInProcessDataObjectAsync() => throw new NotSupportedException();

	public async Task<IAsyncDataTransfer?> TryGetInProcessDataAsync()
	{
		if (_lastDataTransfer is null || _clipboardImpl is not IOwnedClipboardImpl ownedClipboardImpl)
			return null;

		if (!await ownedClipboardImpl.IsCurrentOwnerAsync())
			_lastDataTransfer = null;

		return _lastDataTransfer;
	}
}