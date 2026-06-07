namespace TruthDoctor;

public class ValidationRow
{
    public string Category { get; set; } = "";
    public string Rule { get; set; } = "";
    public string ResourceType { get; set; } = "";
    public string ResourceId { get; set; } = "";
    public string Region { get; set; } = "";
    public string CidrOrDestination { get; set; } = "";
    public string RouteTableId { get; set; } = "";
    public string Status { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Message { get; set; } = "";
}
