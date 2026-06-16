using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Store;

/// <summary>A single requested item on a store requisition. <see cref="RequestedQtyBase"/> is the requested
/// quantity converted to the item's base unit. <see cref="ApprovedQtyBase"/> is set when the requisition is
/// approved (defaults to the requested quantity). <see cref="IssuedQtyBase"/> accumulates as posted issues
/// fulfil the line.</summary>
public class StoreRequisitionLine : BaseEntity
{
    public Guid StoreRequisitionId { get; set; }
    public Guid StoreItemId { get; set; }
    public string ItemName { get; set; } = default!;
    public decimal RequestedQty { get; set; }
    public Guid UnitId { get; set; }
    public decimal RequestedQtyBase { get; set; }
    public decimal ApprovedQtyBase { get; set; }
    public decimal IssuedQtyBase { get; set; }

    /// <summary>Approved (or, before approval, requested) quantity still awaiting issue.</summary>
    public decimal OutstandingQtyBase => Math.Max(0m, ApprovedQtyBase - IssuedQtyBase);
}
