using BornoBit.Restaurant.Application.Accounting.Journals;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Accounts.Gl;

public partial class Journals : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<JournalEntryListItemDto> _items = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            var result = await Mediator.Send(new GetJournalEntriesQuery());
            _items = result.Items.ToList();
        }
        catch (Exception ex) { _error = $"Failed to load journal: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task NewAsync()
    {
        var result = await DialogService.ShowAsync<JournalEntryDialog, JournalEntryFormModel>(
            new JournalEntryFormModel(), new BoDialogOptions
            {
                Title = "New journal entry",
                Width = "720px",
                DismissOnOverlayClick = false
            });
        if (!result.Cancelled && result.Data is JournalEntryFormModel m && m.Saved)
        {
            ToastService.ShowSuccess("Journal entry posted.");
            await ReloadAsync();
        }
    }

    private async Task VoidAsync(JournalEntryListItemDto e)
    {
        var ok = await DialogService.ConfirmAsync(
            "Void journal entry", $"Void {e.EntryNumber}? It will no longer affect balances.", "Void", "Cancel", "danger");
        if (!ok) return;
        try
        {
            await Mediator.Send(new VoidJournalEntryCommand(e.Id));
            ToastService.ShowSuccess($"{e.EntryNumber} voided.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }

    private static string StatusColor(JournalStatus s) => s switch
    {
        JournalStatus.Posted => "var(--bo-success, #16a34a)",
        JournalStatus.Void => "var(--bo-text-muted)",
        _ => "var(--bo-warning, #d97706)"
    };
}
