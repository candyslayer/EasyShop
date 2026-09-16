namespace Mall.Api.Domain.Orders;

public sealed class FreightRule
{
    public long Id { get; set; }
    public string Region { get; set; } = "全国";
    public decimal BaseAmount { get; set; }
    public decimal FreeThreshold { get; set; }
    public bool Enabled { get; set; } = true;
}
