using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using AvaloniaApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AvaloniaApp;

public class ViewLocator : IDataTemplate {

	public Control? Build(object? data) {
		if (data is null)
			return null;

		var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
		var type = Type.GetType(name);

		if (type != null) {
			var control = (Control)DI.Services.GetRequiredService(type)!;
			control.DataContext = data;
			return control;
		}

		return new TextBlock { Text = "Not Found: " + name };
	}

	public bool Match(object? data) {
		return data is ViewModelBase;
	}
}
