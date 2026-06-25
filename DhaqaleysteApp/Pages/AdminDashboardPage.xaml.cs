namespace DhaqaleysteApp.Pages;

public partial class AdminDashboardPage : ContentPage
{
    public AdminDashboardPage()
    {
        InitializeComponent();
    }

    private async void AddProduct_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddProductPage());
    }
    private async void ManageProducts_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ManageProductsPage());
    }
}