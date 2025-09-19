using Avalonia.Controls;

namespace TestApp;
public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
	}

	private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
	{
		Window child = new Child();
		child.ShowDialog(this);
	}
}