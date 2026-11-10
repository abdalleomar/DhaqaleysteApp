using System.ComponentModel.DataAnnotations;

namespace DhaqaleysteApi.Dtos
{
    /// <summary>Like [EmailAddress], but an empty value is allowed (the field is optional).</summary>
    public sealed class OptionalEmailAttribute : ValidationAttribute
    {
        private static readonly EmailAddressAttribute Email = new();

        public OptionalEmailAttribute() : base("Enter a valid email address or leave it empty.") { }

        public override bool IsValid(object? value) =>
            value is not string text || string.IsNullOrWhiteSpace(text) || Email.IsValid(text.Trim());
    }

    /// <summary>Like [Phone], but an empty value is allowed (the field is optional).</summary>
    public sealed class OptionalPhoneAttribute : ValidationAttribute
    {
        private static readonly PhoneAttribute PhoneNumber = new();

        public OptionalPhoneAttribute() : base("Enter a valid phone number or leave it empty.") { }

        public override bool IsValid(object? value) =>
            value is not string text || string.IsNullOrWhiteSpace(text) || PhoneNumber.IsValid(text.Trim());
    }
}
