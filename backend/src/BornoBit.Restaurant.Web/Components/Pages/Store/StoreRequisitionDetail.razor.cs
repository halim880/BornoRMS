using BornoBit.Restaurant.Application.Store.Requisitions;
using BornoBit.Restaurant.Domain.Store;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public partial class StoreRequisitionDetail : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private bool _busy;
    private string? _error;
    private StoreRequisitionDetailDto? _req;

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _req = await Mediator.Send(new GetStoreRequisitionQuery(Id));
        }
        catch (Exception ex) { _error = $"Failed to load requisition: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task SubmitAsync()
    {
        _busy = true;
        try
        {
            await Mediator.Send(new SubmitStoreRequisitionCommand(Id));
            ToastService.ShowSuccess("Requisition submitted for approval.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
        finally { _busy = false; }
    }

    private async Task CancelAsync()
    {
        _busy = true;
        try
        {
            await Mediator.Send(new CancelStoreRequisitionCommand(Id));
            ToastService.ShowSuccess("Requisition cancelled.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
        finally { _busy = false; }
    }

    private async Task ShowApproveAsync()
    {
        if (_req is null) return;
        var model = new StoreRequisitionApproveModel
        {
            RequisitionId = Id,
            Lines = _req.Lines.Select(l => new StoreRequisitionApproveModel.Line
            {
                LineId = l.Id,
                ItemName = l.ItemName,
                RequestedQtyBase = l.RequestedQtyBase,
                BaseUnitCode = l.BaseUnitCode,
                ApprovedQtyBase = l.RequestedQtyBase
            }).ToList()
        };
        var result = await DialogService.ShowAsync<StoreRequisitionApproveDialog, StoreRequisitionApproveModel>(model, new BoDialogOptions
        {
            Title = $"Approve · {_req.RequisitionNumber}",
            Width = "560px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is StoreRequisitionApproveModel saved && saved.Approved)
        {
            ToastService.ShowSuccess("Requisition approved.");
            await ReloadAsync();
        }
    }

    private async Task ShowRejectAsync()
    {
        if (_req is null) return;
        var model = new StoreRequisitionRejectModel { RequisitionId = Id };
        var result = await DialogService.ShowAsync<StoreRequisitionRejectDialog, StoreRequisitionRejectModel>(model, new BoDialogOptions
        {
            Title = $"Reject · {_req.RequisitionNumber}",
            Width = "480px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is StoreRequisitionRejectModel saved && saved.Rejected)
        {
            ToastService.ShowSuccess("Requisition rejected.");
            await ReloadAsync();
        }
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
