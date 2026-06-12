using BornoBit.Restaurant.Application.Accounting.CashAccounts;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Accounts;

public partial class CashAccounts : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<CashAccountDto> _accounts = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _accounts = (await Mediator.Send(new GetCashAccountsQuery())).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load accounts: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task ShowCreateAsync()
    {
        var model = new CashAccountFormModel();
        var result = await DialogService.ShowAsync<CashAccountFormDialog, CashAccountFormModel>(model, new BoDialogOptions
        {
            Title = "New cash account",
            Width = "480px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is CashAccountFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess($"Cash account '{saved.Name}' created.");
            await ReloadAsync();
        }
    }

    private async Task ShowEditAsync(CashAccountDto a)
    {
        var model = new CashAccountFormModel
        {
            Id = a.Id,
            Name = a.Name,
            Kind = a.Kind,
            OpeningBalance = a.OpeningBalance
        };
        var result = await DialogService.ShowAsync<CashAccountFormDialog, CashAccountFormModel>(model, new BoDialogOptions
        {
            Title = $"Edit · {a.Name}",
            Width = "480px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is CashAccountFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess("Cash account updated.");
            await ReloadAsync();
        }
    }

    private async Task ToggleActiveAsync(CashAccountDto a, bool active)
    {
        try
        {
            await Mediator.Send(new SetCashAccountActiveCommand(a.Id, active));
            ToastService.ShowSuccess($"Cash account '{a.Name}' {(active ? "activated" : "deactivated")}.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }
}
