namespace LawConnect.Shared.Constants;

public static class Roles
{
    public const string Customer = "Customer";
    public const string Lawyer = "Lawyer";
    public const string Admin = "Admin";

    public static readonly IReadOnlyCollection<string> All =
    [
        Customer,
        Lawyer,
        Admin
    ];
}
