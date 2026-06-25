using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using DhaqaleysteApp.Models;

namespace DhaqaleysteApp.Services;

public static class ProductStore
{
    public static ObservableCollection<Product> Products { get; set; } = new();
}
