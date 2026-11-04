using System.Text.Json;

namespace WO_CM_Review_Automation.Web.Services;

public class UserProfileData
{
    public string FullName { get; set; } = "Md Amirul Islam";
    public string Email { get; set; } = "amirul.islam@banglalink.net";
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin123";
    public string Role { get; set; } = "Administrator";
    public string JobTitle { get; set; } = "Lead Engineer · Change Management";
    public string Division { get; set; } = "Business Support · Service & Operations";
    public string Department { get; set; } = "Support Service";
    public string Phone { get; set; } = "+880 1911-000000";
    public string AvatarInitials { get; set; } = "AI";
    public string DefaultDocumentVersion { get; set; } = "1.0";
    public string DefaultTemplateVersion { get; set; } = "1.0";
    public bool AutoShowTotalRow { get; set; } = true;
    public bool NotifyOnCycleCompletion { get; set; } = true;
    public bool CompactTableDensity { get; set; } = false;
    public DateTimeOffset LastPasswordChanged { get; set; } = DateTimeOffset.UtcNow.AddDays(-14);
}

public class UserSessionService
{
    private readonly string _profileFilePath;
    private readonly object _lock = new();

    public UserProfileData Profile { get; private set; } = new();
    public bool IsAuthenticated { get; private set; } = false;
    public DateTimeOffset? LastLoginTime { get; private set; }

    public event Action? OnStateChanged;

    public UserSessionService(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }
        _profileFilePath = Path.Combine(dataDir, "user-profile.json");
        LoadProfile();
    }

    private void LoadProfile()
    {
        try
        {
            if (File.Exists(_profileFilePath))
            {
                var json = File.ReadAllText(_profileFilePath);
                var loaded = JsonSerializer.Deserialize<UserProfileData>(json);
                if (loaded != null)
                {
                    Profile = loaded;
                    EnsureInitials();
                }
            }
            else
            {
                SaveProfileToDisk();
            }
        }
        catch
        {
            Profile = new UserProfileData();
        }
    }

    private void SaveProfileToDisk()
    {
        try
        {
            lock (_lock)
            {
                EnsureInitials();
                var json = JsonSerializer.Serialize(Profile, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_profileFilePath, json);
            }
        }
        catch
        {
            // Ignore IO errors
        }
    }

    private void EnsureInitials()
    {
        if (string.IsNullOrWhiteSpace(Profile.FullName))
        {
            Profile.AvatarInitials = "AI";
            return;
        }

        var parts = Profile.FullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !p.Equals("Md", StringComparison.OrdinalIgnoreCase) && !p.Equals("Md.", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (parts.Count >= 2)
        {
            Profile.AvatarInitials = $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
        }
        else if (parts.Count == 1 && parts[0].Length >= 2)
        {
            Profile.AvatarInitials = parts[0].Substring(0, 2).ToUpperInvariant();
        }
        else
        {
            Profile.AvatarInitials = "AI";
        }
    }

    public (bool Success, string ErrorMessage) ValidateAndLogin(string identifier, string password)
    {
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Please enter your username/email and password.");
        }

        var cleanId = identifier.Trim();
        bool matchesUser =
            cleanId.Equals(Profile.Username, StringComparison.OrdinalIgnoreCase) ||
            cleanId.Equals(Profile.Email, StringComparison.OrdinalIgnoreCase) ||
            cleanId.Equals(Profile.FullName, StringComparison.OrdinalIgnoreCase);

        if (matchesUser && password == Profile.Password)
        {
            IsAuthenticated = true;
            LastLoginTime = DateTimeOffset.Now;
            NotifyStateChanged();
            return (true, string.Empty);
        }

        return (false, "Invalid credentials. Use your configured username/email and password.");
    }

    public void MarkAuthenticatedFromSession()
    {
        if (!IsAuthenticated)
        {
            IsAuthenticated = true;
            LastLoginTime ??= DateTimeOffset.Now;
            NotifyStateChanged();
        }
    }

    public void Logout()
    {
        IsAuthenticated = false;
        NotifyStateChanged();
    }

    public void UpdateProfile(UserProfileData updated)
    {
        Profile.FullName = updated.FullName.Trim();
        Profile.Email = updated.Email.Trim();
        Profile.Username = updated.Username.Trim();
        Profile.Role = updated.Role.Trim();
        Profile.JobTitle = updated.JobTitle.Trim();
        Profile.Division = updated.Division.Trim();
        Profile.Department = updated.Department.Trim();
        Profile.Phone = updated.Phone.Trim();
        Profile.DefaultDocumentVersion = string.IsNullOrWhiteSpace(updated.DefaultDocumentVersion) ? "1.0" : updated.DefaultDocumentVersion.Trim();
        Profile.DefaultTemplateVersion = string.IsNullOrWhiteSpace(updated.DefaultTemplateVersion) ? "1.0" : updated.DefaultTemplateVersion.Trim();
        Profile.AutoShowTotalRow = updated.AutoShowTotalRow;
        Profile.NotifyOnCycleCompletion = updated.NotifyOnCycleCompletion;
        Profile.CompactTableDensity = updated.CompactTableDensity;

        SaveProfileToDisk();
        NotifyStateChanged();
    }

    public (bool Success, string Message) ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword))
            return (false, "Current password is required.");

        if (currentPassword != Profile.Password)
            return (false, "Current password is incorrect.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            return (false, "New password must be at least 4 characters.");

        if (newPassword != confirmPassword)
            return (false, "New password and confirmation do not match.");

        Profile.Password = newPassword;
        Profile.LastPasswordChanged = DateTimeOffset.UtcNow;
        SaveProfileToDisk();
        NotifyStateChanged();
        return (true, "Password updated successfully.");
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}

