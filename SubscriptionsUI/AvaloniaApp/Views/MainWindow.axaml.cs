using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using AvaloniaApp.ViewModels;
using ReactiveUI;

namespace AvaloniaApp.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel> {
	public MainWindow() {
		this.WhenActivated((d) => { });
		AvaloniaXamlLoader.Load(this);
	}
}
