namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public class StoreRequisitionApproveModel
{
    public Guid RequisitionId { get; set; }
    public List<Line> Lines { get; set; } = new();
    public bool Approved { get; set; }

    public class Line
    {
        public Guid LineId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal RequestedQtyBase { get; set; }
        public string BaseUnitCode { get; set; } = string.Empty;
        public decimal ApprovedQtyBase { get; set; }
    }
}
