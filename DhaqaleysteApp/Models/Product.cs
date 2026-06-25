using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DhaqaleysteApp.Models
{
    public class Product : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _productName = "";
        private string _description = "";
        private decimal _price;
        private int _quantity;
        private string _contact = "";
        private string _location = "";
        private string _category = "";
        private string _imagePath = "";

        [PrimaryKey, AutoIncrement]
        public int ProductId { get; set; }

        public string ProductName
        {
            get => _productName;
            set { _productName = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public decimal Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); }
        }

        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); }
        }

        public string Contact
        {
            get => _contact;
            set { _contact = value; OnPropertyChanged(); }
        }
        public string VideoPath { get; set; } = "";
        public string Location
        {
            get => _location;
            set { _location = value; OnPropertyChanged(); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); }
        }

        public string ImagePath
        {
            get => _imagePath;
            set { _imagePath = value; OnPropertyChanged(); }
        }

        public string ImageUrl { get; set; } = "";
        public string VideoUrl { get; set; } = "";

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);
    }
}