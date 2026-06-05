using System.Windows;
using PdfAutoPrint.Pro.ViewModels;

namespace PdfAutoPrint.Pro.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
