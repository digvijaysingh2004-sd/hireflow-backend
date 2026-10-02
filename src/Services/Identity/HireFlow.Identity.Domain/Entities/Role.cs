namespace HireFlow.Identity.Domain.Entities;

public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    // Constant role names matching blueprint specifications
    public const string Admin = "Admin";
    public const string Recruiter = "Recruiter";
    public const string HiringManager = "HiringManager";
    public const string Candidate = "Candidate";
}
