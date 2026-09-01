namespace CareerOS.Api.Infrastructure.OpenBao;

public class OpenBaoOptions
{
    public bool Enabled { get; set; }
    public string Address { get; set; } = "http://localhost:8200";
    public string? RoleId { get; set; }
    public string? SecretId { get; set; }
    public string MountPoint { get; set; } = "secret";
}
