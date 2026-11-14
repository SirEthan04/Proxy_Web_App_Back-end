using System.Net.Mail;
namespace BodeGo.API.IAM.Domain.Model.ValueObjects;

public sealed record EmailAddress
{
    public string Value { get; }
    public EmailAddress(string value)
    {
        value = value?.Trim() ?? "";
        if (value.Length > 180 || !MailAddress.TryCreate(value, out var address) ||
            address.Address != value || !value.Contains('@'))
            throw new ArgumentException("Invalid email address.", nameof(value));
        Value = value.ToLowerInvariant();
    }
}
