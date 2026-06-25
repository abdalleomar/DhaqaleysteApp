using System.Collections.ObjectModel;
using DhaqaleysteApp.Models;

namespace DhaqaleysteApp;

public partial class MainPage : ContentPage
{
    public ObservableCollection<Product> Products { get; set; } = new();

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private async void Admin_Clicked(object sender, EventArgs e)
    {
        await DisplayAlert("Admin", "Admin Dashboard Coming Next", "OK");
    }
}