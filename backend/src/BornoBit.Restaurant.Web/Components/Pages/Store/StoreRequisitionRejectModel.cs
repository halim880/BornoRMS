namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public class StoreRequisitionRejectModel
{
    public Guid RequisitionId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool Rejected { get; set; }
}
