using System.Collections.ObjectModel;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

public partial class CategoriesPage : ContentPage
{
    private readonly ApiService _api = ServiceHelper.Get<ApiService>();

    public ObservableCollection<Category> Categories { get; } = new();

    public CategoriesPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var result = await _api.GetCategoriesAsync(includeHidden: true);
        Refresh.IsRefreshing = false;
        LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = false;
        EmptyLabel.IsVisible = true;

        if (!result.Success)
        {
            EmptyLabel.Text = result.Error;
            return;
        }

        Categories.Clear();
        foreach (var category in result.Data!)
            Categories.Add(category);
    }

    private async void Refresh_Refreshing(object? sender, EventArgs e) => await LoadAsync();

    private async void Add_Clicked(object sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("New category", "Name in English", "Next", "Cancel", maxLength: 100);
        if (string.IsNullOrWhiteSpace(name))
            return;

        var nameSo = await DisplayPromptAsync("New category", "Name in Somali (optional)", "Save", "Skip", maxLength: 100) ?? "";
        await SaveAsync(new Category { Name = name.Trim(), NameSo = nameSo.Trim(), SortOrder = Categories.Count, IsActive = true });
    }

    private async void Category_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject { BindingContext: Category category })
            return;

        const string rename = "Rename (English)";
        const string somali = "Somali name";
        const string icon = "Icon (emoji)";
        var visibility = category.IsActive ? "Hide from customers" : "Show to customers";

        var choice = await DisplayActionSheet(category.Name, "Cancel", "Delete", rename, somali, icon, visibility);
        var copy = Copy(category);

        switch (choice)
        {
            case rename:
                var name = await DisplayPromptAsync("Rename", "Name in English", "Save", "Cancel", initialValue: category.Name, maxLength: 100);
                if (string.IsNullOrWhiteSpace(name)) return;
                copy.Name = name.Trim();
                break;
            case somali:
                var nameSo = await DisplayPromptAsync("Somali name", "Name in Somali", "Save", "Cancel", initialValue: category.NameSo, maxLength: 100);
                if (nameSo == null) return;
                copy.NameSo = nameSo.Trim();
                break;
            case icon:
                var emoji = await DisplayPromptAsync("Icon", "An emoji shown on the category chip, e.g. 📱", "Save", "Cancel", initialValue: category.Icon, maxLength: 8);
                if (emoji == null) return;
                copy.Icon = emoji.Trim();
                break;
            case "Delete":
                await DeleteAsync(category);
                return;
            default:
                if (choice != visibility) return;
                copy.IsActive = !category.IsActive;
                break;
        }

        await SaveAsync(copy);
    }

    private async Task SaveAsync(Category category)
    {
        var result = await _api.SaveCategoryAsync(category);
        if (result.Success)
            await LoadAsync();
        else
            await DisplayAlert("Not saved", result.Error, "OK");
    }

    private async Task DeleteAsync(Category category)
    {
        if (!await DisplayAlert("Delete", $"Delete \"{category.Name}\"?", "Delete", "Cancel"))
            return;

        var result = await _api.DeleteCategoryAsync(category.CategoryId);
        if (result.Success)
            Categories.Remove(category);
        else
            await DisplayAlert("Not deleted", result.Error, "OK");
    }

    private static Category Copy(Category c) => new()
    {
        CategoryId = c.CategoryId,
        Name = c.Name,
        NameSo = c.NameSo,
        Icon = c.Icon,
        SortOrder = c.SortOrder,
        IsActive = c.IsActive
    };
}
