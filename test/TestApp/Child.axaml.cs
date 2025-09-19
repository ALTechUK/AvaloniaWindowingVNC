using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TestApp;

public partial class Child : Window
{
    public Child()
    {
        InitializeComponent();
    }

	private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close("Result");
    }
}