using System.Windows;
using System.Windows.Input;
using HeatWatch.ViewModels;

namespace HeatWatch.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.SaveCommand.Execute(this);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
