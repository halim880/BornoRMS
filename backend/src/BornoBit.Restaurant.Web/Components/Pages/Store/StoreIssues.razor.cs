using BornoBit.Restaurant.Application.Store.Departments;
using BornoBit.Restaurant.Application.Store.Issues;
using BornoBit.Restaurant.Domain.Store;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public partial class StoreIssues : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private bool _posting;
    private string? _error;
    private List<StoreIssueListItemDto> _rows = new();
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
            var result = await Mediator.Send(new GetStoreIssuesQuery(StoreDepartmentId: _filterDepartmentId, PageSize: 100));
            _rows = result.Items.ToList();
        }
        catch (Exception ex) { _error = $"Failed to load issues: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task OnFilterDepartmentChanged(StoreDepartmentDto? d)
    {
        _filterDepartmentId = d?.Id;
        await ReloadAsync();
    }

    private async Task PostAsync(StoreIssueListItemDto g)
    {
        _posting = true;
        try
        {
            await Mediator.Send(new PostStoreIssueCommand(g.Id));
            ToastService.ShowSuccess($"{g.IssueNumber} posted — stock issued.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
        finally { _posting = false; }
    }

    private async Task VoidAsync(StoreIssueListItemDto g)
    {
        _posting = true;
        try
        {
            await Mediator.Send(new VoidStoreIssueCommand(g.Id, null));
            ToastService.ShowSuccess($"{g.IssueNumber} voided — stock restored.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
        finally { _posting = false; }
    }

    private static string StatusTone(StoreIssueStatus status) => status switch
    {
        StoreIssueStatus.Posted => "success",
        StoreIssueStatus.Voided => "neutral",
        _ => "warning"
    };
}
