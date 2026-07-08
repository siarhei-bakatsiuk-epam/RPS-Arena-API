namespace RpsArena.Match.Domain.Entities;

/// <summary>
/// A registered participant in the arena. Aggregate root for the players table.
/// </summary>
public class Player
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    // Required by EF Core materialization.
    private Player()
    {
    }

    public Player(Guid id, string username, string email, DateTime createdAt)
    {
        Id = id;
        Username = username;
        Email = email;
        CreatedAt = createdAt;
    }

    public static Player Register(string username, string email, DateTime createdAtUtc)
        => new(Guid.NewGuid(), username, email, createdAtUtc);
}
