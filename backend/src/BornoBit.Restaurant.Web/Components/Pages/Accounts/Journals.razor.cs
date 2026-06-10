using BornoBit.Restaurant.Application.Accounting.Journals;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Accounts;

public partial class Journals : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<JournalEntryListItemDto> _items = new();

    private DateTime? _from;
    private DateTime? _to;

    public record StatusOption(string Key, string Label, JournalStatus? Value);

    private readonly List<StatusOption> _statusOptions = new()
    {
        new("", "All", null),
        new("Draft", "Draft", JournalStatus.Draft),
        new("Posted", "Posted", JournalStatus.Posted),
        new("Void", "Void", JournalStatus.Void),
    };
    private StatusOption _selectedStatus = null!;

    protected override Task OnInitializedAsync()
    {
        _selectedStatus = _statusOptions[0];
        return ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            var result = await Mediator.Send(new GetJournalEntriesQuery(
                FromDate: _from, ToDate: _to, Status: _selectedStatus?.Value, PageSize: 200));
            _items = result.Items.ToList();
        }
        catch (Exception ex) { _error = $"Failed to load entries: {ex.Message}"; }
        finally { _loading = false; }
    }

    private Task OnFromChanged(DateTime? d) { _from = d; return ReloadAsync(); }
    private Task OnToChanged(DateTime? d) { _to = d; return ReloadAsync(); }
    private Task OnStatusChanged(StatusOption? o) { _selectedStatus = o ?? _statusOptions[0]; return ReloadAsync(); }

    private async Task PostAsync(JournalEntryListItemDto e)
    {
        try
        {
            await Mediator.Send(new PostJournalEntryCommand(e.Id));
            ToastService.ShowSuccess($"{e.EntryNumber} posted.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }

    private async Task VoidAsync(JournalEntryListItemDto e)
    {
        var ok = await DialogService.ConfirmAsync(
            "Void entry", $"Void {e.EntryNumber}? This removes it from ledger balances.", "Void", "Cancel", "danger");
        if (!ok) return;
        try
        {
            await Mediator.Send(new VoidJournalEntryCommand(e.Id));
            ToastService.ShowSuccess($"{e.EntryNumber} voided.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }

    private static string ToneFor(JournalStatus s) => s switch
    {
        JournalStatus.Draft => "warning",
        JournalStatus.Posted => "success",
        JournalStatus.Void => "danger",
        _ => "neutral"
    };
}
