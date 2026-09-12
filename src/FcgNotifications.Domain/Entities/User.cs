using FcgUsers.Domain.ValueObjects;

namespace FcgNotifications.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public Email Email { get; private set; }

    protected User()
    {
        Email = null!;
    }

    public User(Guid id, string name, Email email)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
        Name = name;
        Email = email;
    }

    public User(string name, Email email)
        : this(Guid.NewGuid(), name, email) { }
}
