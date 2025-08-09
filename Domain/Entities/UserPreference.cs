namespace Domain.Entities;
public class UserPreference
{
    public Guid UserId { get; set; }
    public string Units { get; set; } = "metric";
    public string? LlmBio { get; set; }
    public User User { get; set; } = default!;
}
