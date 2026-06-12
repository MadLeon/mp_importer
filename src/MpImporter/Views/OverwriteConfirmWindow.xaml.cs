using System.Windows;
using MpImporter.ViewModels;

namespace MpImporter.Views;

public partial class OverwriteConfirmWindow : Window
{
    public OverwriteConfirmWindow()
    {
        InitializeComponent();
    }

    private void Overwrite_Click(object sender, RoutedEventArgs e)
    {
        ((OverwriteConfirmViewModel)DataContext).Overwrite();
        DialogResult = true;
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        ((OverwriteConfirmViewModel)DataContext).Skip();
        DialogResult = false;
    }
}
