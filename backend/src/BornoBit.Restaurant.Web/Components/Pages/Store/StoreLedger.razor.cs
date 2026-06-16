using BornoBit.Restaurant.Application.Store.Items;
using BornoBit.Restaurant.Application.Store.Ledger;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public partial class StoreLedger : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<StoreItemDto> _items = new();
    private StoreMovementLedgerDto? _data;

    private Guid? _itemId;
    private DateTime? _from;
    private DateTime? _to;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _items = (await Mediator.Send(new GetStoreItemsQuery(PageSize: 200))).Items.ToList();
        }
        catch (Exception ex) { _error = $"Failed to load items: {ex.Message}"; }
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _data = await Mediator.Send(new GetStoreMovementLedgerQuery(_itemId, ToUtc(_from), ToUtc(_to, endOfDay: true)));
        }
        catch (Exception ex) { _error = $"Failed to load ledger: {ex.Message}"; }
        finally { _loading = false; }
    }

    private static DateTime? ToUtc(DateTime? local, bool endOfDay = false)
    {
        if (local is null) return null;
        var d = DateTime.SpecifyKind(local.Value.Date, DateTimeKind.Utc);
        return endOfDay ? d.AddDays(1) : d;
    }

    private string PdfUrl()
    {
        var qs = new List<string>();
        if (_itemId is { } id) qs.Add($"itemId={id}");
        if (ToUtc(_from) is { } f) qs.Add($"fromUtc={f:o}");
        if (ToUtc(_to, endOfDay: true) is { } t) qs.Add($"toUtc={t:o}");
        return "/reports/store/ledger.pdf" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
    }
}
