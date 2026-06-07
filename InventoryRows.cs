
namespace TruthDoctor;

public class VpcInventoryRow
{
    public string VpcId { get; set; } = "";
    public string Cidr { get; set; } = "";
    public string State { get; set; } = "";
    public string Region { get; set; } = "";
}

public class SubnetInventoryRow
{
    public string SubnetId { get; set; } = "";
    public string VpcId { get; set; } = "";
    public string Cidr { get; set; } = "";
    public string AvailabilityZone { get; set; } = "";
    public string State { get; set; } = "";
    public string Action { get; set; } = "";
    public string Region { get; set; } = "";
}

public class RouteTableInventoryRow
{
    public string RouteTableId { get; set; } = "";
    public string VpcId { get; set; } = "";
    public bool IsMain { get; set; }
    public string AssociationType { get; set; } = "";
    public string AssociatedSubnetId { get; set; } = "";
    public string DestinationCidr { get; set; } = "";
    public string Region { get; set; } = "";
}
