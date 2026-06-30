using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace DocumentStorage.Domain.Entities;

/// <summary>
/// Represents a tenant project. All files are scoped to a project.
/// Each project identifies itself via an API key.
/// </summary>
public class Project
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string FolderName { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string ApiKey { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Project() { }

    public static Project Create(string name, string folderName, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name is required.", nameof(name));

        var sanitizedFolder = ValidateFolderName(folderName);

        return new Project
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            FolderName = sanitizedFolder,
            Description = description?.Trim() ?? string.Empty,
            ApiKey = GenerateApiKey(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string? name = null, string? description = null)
    {
        if (name is not null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Project name cannot be empty or whitespace.", nameof(name));
            Name = name.Trim();
        }
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

    /// <summary>
    /// Validates and normalizes a folder name. Must be ASCII-only, no spaces, no
    /// Vietnamese diacritics, no path separators — keeps folder names safe across
    /// object-storage providers, file systems, and URL paths.
    /// Allowed: letters a-z A-Z, digits 0-9, hyphen '-', underscore '_'.
    /// </summary>
    private static string ValidateFolderName(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            throw new ArgumentException("Folder name is required.", nameof(folderName));

        var trimmed = folderName.Trim();

        if (trimmed.Length > 100)
            throw new ArgumentException("Folder name cannot exceed 100 characters.", nameof(folderName));

        if (!FolderNamePattern.IsMatch(trimmed))
            throw new ArgumentException(
                "Folder name may only contain ASCII letters, digits, hyphen '-' or underscore '_' " +
                "(no spaces, no Vietnamese diacritics, no special characters).",
                nameof(folderName));

        return trimmed;
    }

    private static readonly Regex FolderNamePattern =
        new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    private static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return $"pk_{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }
}
