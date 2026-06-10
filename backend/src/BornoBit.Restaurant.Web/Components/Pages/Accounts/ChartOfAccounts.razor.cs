using BornoBit.Restaurant.Application.Accounting.Accounts;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Accounts;

public partial class ChartOfAccounts : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<AccountDto> _accounts = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _accounts = (await Mediator.Send(new GetAccountsQuery())).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load accounts: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task ShowCreateAsync()
    {
        var model = new AccountFormModel();
        var result = await DialogService.ShowAsync<AccountFormDialog, AccountFormModel>(model, new BoDialogOptions
        {
            Title = "New account",
            Width = "520px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is AccountFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess($"Account '{saved.Name}' created.");
            await ReloadAsync();
        }
    }

    private async Task ShowEditAsync(AccountDto a)
    {
        var model = new AccountFormModel
        {
            Id = a.Id,
            Code = a.Code,
            Name = a.Name,
            AccountType = a.AccountType,
            ParentId = a.ParentId,
            IsPostable = a.IsPostable,
            Description = a.Description
        };
        var result = await DialogService.ShowAsync<AccountFormDialog, AccountFormModel>(model, new BoDialogOptions
        {
            Title = $"Edit account · {a.Code}",
            Width = "520px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is AccountFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess("Account updated.");
            await ReloadAsync();
        }
    }

    private async Task ToggleActiveAsync(AccountDto a, bool active)
    {
        try
        {
            await Mediator.Send(new SetAccountActiveCommand(a.Id, active));
            ToastService.ShowSuccess($"Account '{a.Name}' {(active ? "activated" : "deactivated")}.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }
}
