using BornoBit.Restaurant.Application.Accounting.Accounts;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Accounts.Gl;

public partial class ChartOfAccounts : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private IReadOnlyList<AccountNodeDto> _nodes = Array.Empty<AccountNodeDto>();
    private List<AccountDto> _flat = new();
    private readonly HashSet<Guid> _expanded = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _nodes = await Mediator.Send(new GetChartOfAccountsTreeQuery());
            _flat = (await Mediator.Send(new GetAccountsQuery())).ToList();
            // Expand the top level by default so the chart isn't a wall of collapsed roots.
            foreach (var n in _nodes) _expanded.Add(n.Id);
        }
        catch (Exception ex) { _error = $"Failed to load chart of accounts: {ex.Message}"; }
        finally { _loading = false; }
    }

    private bool IsExpanded(Guid id) => _expanded.Contains(id);
    private void Toggle(Guid id) { if (!_expanded.Add(id)) _expanded.Remove(id); }
    private void ExpandAll() { foreach (var a in _flat) _expanded.Add(a.Id); }
    private void CollapseAll() => _expanded.Clear();

    private Task AddRootAsync() => ShowFormAsync(new AccountFormModel { IsPostable = false });

    private Task AddChildAsync(AccountNodeDto parent) =>
        ShowFormAsync(new AccountFormModel { ParentId = parent.Id, AccountType = parent.AccountType, IsPostable = true });

    private Task EditAsync(AccountNodeDto node)
    {
        var dto = _flat.FirstOrDefault(a => a.Id == node.Id);
        var model = new AccountFormModel
        {
            Id = node.Id,
            Code = node.Code,
            Name = node.Name,
            AccountType = node.AccountType,
            ParentId = dto?.ParentId,
            IsPostable = node.IsPostable,
            Description = dto?.Description
        };
        return ShowFormAsync(model);
    }

    private async Task ShowFormAsync(AccountFormModel model)
    {
        var result = await DialogService.ShowAsync<AccountFormDialog, AccountFormModel>(model, new BoDialogOptions
        {
            Title = model.Id is null ? "New account" : $"Edit · {model.Code}",
            Width = "520px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is AccountFormModel m && m.Saved)
        {
            ToastService.ShowSuccess(model.Id is null ? "Account created." : "Account updated.");
            await ReloadAsync();
        }
    }

    private async Task ToggleActiveAsync(AccountNodeDto node)
    {
        try
        {
            await Mediator.Send(new SetAccountActiveCommand(node.Id, !node.IsActive));
            ToastService.ShowSuccess(node.IsActive ? "Account deactivated." : "Account activated.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }
}
