using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Editor.Services;
using Editor.ViewModels;
using Editor.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Editor;

public partial class App : Application
{
    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            var services = new ServiceCollection();
            services.AddSingleton<IFilesService>(x => new FilesService(desktop.MainWindow));

            Services = services.BuildServiceProvider();
        }
        
        base.OnFrameworkInitializationCompleted();
    }
}