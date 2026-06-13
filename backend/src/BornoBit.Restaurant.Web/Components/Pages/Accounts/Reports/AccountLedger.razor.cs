using BornoBit.Restaurant.Application.Accounting.CashAccounts;
using BornoBit.Restaurant.Application.Accounting.Reports;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Accounts.Reports;

public partial class AccountLedger : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private string? _rangeError;

    private CashLedgerDto _report = new(0m, 0m, 0m, 0m, Array.Empty<CashLedgerRowDto>());
    private List<CashAccountDto> _accounts = new();

    private DateTime _from = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
    private DateTime _to = DateTime.UtcNow.Date;
    private Guid? _accountId;

    private CashAccountDto? _selectedAccount => _accounts.FirstOrDefault(a => a.Id == _accountId);

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _accounts = (await Mediator.Send(new GetCashAccountsQuery())).ToList();
            _accountId = _accounts.FirstOrDefault()?.Id; // ledger is per-account; default to the first
        }
        catch (Exception ex) { _error = $"Failed to load accounts: {ex.Message}"; }
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _rangeError = null;
        if (_accountId is null)
        {
            _loading = false;
            _report = new CashLedgerDto(0m, 0m, 0m, 0m, Array.Empty<CashLedgerRowDto>());
            return;
        }
        if (_from > _to)
        {
            _rangeError = "The From date is after the To date.";
            _report = new CashLedgerDto(0m, 0m, 0m, 0m, Array.Empty<CashLedgerRowDto>());
            return;
        }

        _loading = true; _error = null;
        try
        {
            _report = await Mediator.Send(new GetCashLedgerQuery(_from, _to, _accountId));
        }
        catch (Exception ex) { _error = $"Failed to load ledger: {ex.Message}"; }
        finally { _loading = false; }
    }

    private Task OnAccountChanged(CashAccountDto? a) { _accountId = a?.Id; return ReloadAsync(); }
    private Task OnFromChanged(DateTime? d) { if (d.HasValue) _from = d.Value.Date; return ReloadAsync(); }
    private Task OnToChanged(DateTime? d) { if (d.HasValue) _to = d.Value.Date; return ReloadAsync(); }
}
