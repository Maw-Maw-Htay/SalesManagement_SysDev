namespace SalesManagement_SysDev;

public static class AppSession
{
    public static MEmployee? Employee { get; set; }
    public static string Role => Employee?.Po?.PoName ?? "";
    public static bool IsAdmin => Role == "管理者";
    public static bool IsSales => Role == "営業";
    public static bool IsLogistics => Role == "物流";
}
