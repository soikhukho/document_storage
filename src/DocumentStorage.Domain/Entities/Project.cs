using System.Security.Cryptography;

namespace DocumentStorage.Domain.Entities;

/// <summary>
/// Represents a tenant project. All files are scoped to a project.
/// Each project identifies itself via an API key.
/// </summary>
public class Project
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string ApiKey { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Project() { }

    public static Project Create(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name is required.", nameof(name));

        return new Project
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            ApiKey = GenerateApiKey(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string? name = null, string? description = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name.Trim();
        if (description is not null)
            Description = description.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RegenerateApiKey()
    {
        ApiKey = GenerateApiKey();
        UpdatedAt = DateTime.UtcNow;
    }

    private static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return $"pk_{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }
}
