using System.Text.Json.Serialization;
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
        private string _imageUrl = "";

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

        /// <summary>Full URL of <see cref="ImagePath"/>, filled in by ApiService. Bind images to this.</summary>
        [JsonIgnore]
        public string ImageUrl
        {
            get => _imageUrl;
            set { _imageUrl = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasImage)); }
        }

        /// <summary>Full URL of <see cref="VideoPath"/>, filled in by ApiService.</summary>
        [JsonIgnore]
        public string VideoUrl { get; set; } = "";

        [JsonIgnore]
        public bool HasVideo => !string.IsNullOrWhiteSpace(VideoUrl);

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        [JsonIgnore]
        public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);

        /// <summary>A copy used for editing, so the list is unchanged until the server accepts the edit.</summary>
        public Product Clone() => new()
        {
            ProductId = ProductId,
            ProductName = ProductName,
            Description = Description,
            Price = Price,
            Quantity = Quantity,
            Contact = Contact,
            Location = Location,
            Category = Category,
            ImagePath = ImagePath,
            VideoPath = VideoPath,
            ImageUrl = ImageUrl,
            VideoUrl = VideoUrl
        };

        /// <summary>Copies values from an edited copy back onto this instance (updates bound UI).</summary>
        public void CopyFrom(Product other)
        {
            ProductName = other.ProductName;
            Description = other.Description;
            Price = other.Price;
            Quantity = other.Quantity;
            Contact = other.Contact;
            Location = other.Location;
            Category = other.Category;
            ImagePath = other.ImagePath;
            VideoPath = other.VideoPath;
            ImageUrl = other.ImageUrl;
            VideoUrl = other.VideoUrl;
        }
    }
}