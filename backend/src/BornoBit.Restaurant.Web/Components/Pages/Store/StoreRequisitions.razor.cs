using BornoBit.Restaurant.Application.Store.Departments;
using BornoBit.Restaurant.Application.Store.Requisitions;
using BornoBit.Restaurant.Domain.Store;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public partial class StoreRequisitions : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<StoreRequisitionListItemDto> _rows = new();
    private List<StoreDepartmentDto> _departments = new();
    private Guid? _filterDepartmentId;

    protected override async Task OnInitializedAsync()
    {
        _departments = (await Mediator.Send(new GetStoreDepartmentsQuery(IncludeInactive: true))).ToList();
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            var result = await Mediator.Send(new GetStoreRequisitionsQuery(StoreDepartmentId: _filterDepartmentId, PageSize: 100));
            _rows = result.Items.ToList();
        }
        catch (Exception ex) { _error = $"Failed to load requisitions: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task OnFilterDepartmentChanged(StoreDepartmentDto? d)
    {
        _filterDepartmentId = d?.Id;
        await ReloadAsync();
    }

    private static string StatusLabel(StoreRequisitionStatus status) => status switch
    {
        StoreRequisitionStatus.PartiallyIssued => "Partially issued",
        _ => status.ToString()
    };

    private static string StatusTone(StoreRequisitionStatus status) => status switch
    {
        StoreRequisitionStatus.Approved => "info",
        StoreRequisitionStatus.Issued => "success",
        StoreRequisitionStatus.PartiallyIssued => "info",
        StoreRequisitionStatus.Rejected => "danger",
        StoreRequisitionStatus.Cancelled => "neutral",
        _ => "warning"
    };
}
