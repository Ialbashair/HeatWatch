using System.Windows;
using System.Windows.Input;
using HeatWatch.ViewModels;

namespace HeatWatch.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _vm;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            _vm = e.NewValue as MainViewModel;
        };
        LocationChanged += OnLocationChanged;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        _vm?.PersistWindowPosition(Left, Top);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _vm?.Dispose();
        base.OnClosed(e);
    }

    protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        _vm?.NotifyMouseEntered();
    }

    protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _vm?.NotifyMouseLeft();
    }

    private void Button_Click(object sender, RoutedEventArgs e) { }
}
