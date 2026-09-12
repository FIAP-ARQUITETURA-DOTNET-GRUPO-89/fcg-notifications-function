namespace FcgUsers.Domain.ValueObjects;

public record Email
{
    public string Address { get; }

    private Email(string address) => Address = address;

    public static Email Create(string address)
    {
        if (string.IsNullOrWhiteSpace(address) ||
            !address.Contains("@") ||
            address.Trim().StartsWith("@") ||
            address.Trim().EndsWith("@"))
        {
            throw new ArgumentException("E-mail inválido.");
        }

        return new Email(address.ToLower().Trim());
    }
}
