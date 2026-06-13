using BornoBit.Restaurant.Application.Accounting.CashAccounts;
using BornoBit.Restaurant.Application.Accounting.Categories;
using BornoBit.Restaurant.Application.Accounting.Reports;
using BornoBit.Restaurant.Application.Accounting.Transactions;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Accounts;

public partial class Transactions : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;

    private List<TransactionListItemDto> _items = new();
    private FinanceSummaryDto _summary = new(0m, 0m, 0m, Array.Empty<CategoryTotalDto>());

    private List<CategoryDto> _categories = new();
    private List<CashAccountDto> _accounts = new();

    private const int PageSize = 50;
    private int _page = 1;
    private int _totalPages = 1;
    private long _totalCount;

    // Filters
    private DateTime? _from;
    private DateTime? _to;
    private TransactionType? _typeFilter;
    private Guid? _categoryFilter;
    private Guid? _accountFilter;

    public record TypeOption(string Key, string Label, TransactionType? Value);

    private readonly List<TypeOption> _typeOptions = new()
    {
        new("", "All", null),
        new("Income", "Income", TransactionType.Income),
        new("Expense", "Expense", TransactionType.Expense),
    };
    private TypeOption _selectedType = null!;

    private CategoryDto? _selectedCategory => _categories.FirstOrDefault(c => c.Id == _categoryFilter);
    private CashAccountDto? _selectedAccount => _accounts.FirstOrDefault(a => a.Id == _accountFilter);

    protected override async Task OnInitializedAsync()
    {
        _selectedType = _typeOptions[0];
        try
        {
            _categories = (await Mediator.Send(new GetCategoriesQuery())).ToList();
            _accounts = (await Mediator.Send(new GetCashAccountsQuery())).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load filters: {ex.Message}"; }
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            var result = await Mediator.Send(new GetTransactionsQuery(
                FromUtc: _from, ToUtc: _to, Type: _typeFilter,
                CategoryId: _categoryFilter, CashAccountId: _accountFilter,
                Page: _page, PageSize: PageSize));
            _items = result.Items.ToList();
            _totalPages = result.TotalPages;
            _totalCount = result.TotalCount;

            _summary = await Mediator.Send(new GetFinanceSummaryQuery(_from, _to));
        }
        catch (Exception ex) { _error = $"Failed to load transactions: {ex.Message}"; }
        finally { _loading = false; }
    }

    private Task OnFromChanged(DateTime? d) { _from = d; _page = 1; return ReloadAsync(); }
    private Task OnToChanged(DateTime? d) { _to = d; _page = 1; return ReloadAsync(); }
    private Task OnTypeChanged(TypeOption? o) { _selectedType = o ?? _typeOptions[0]; _typeFilter = _selectedType.Value; _page = 1; return ReloadAsync(); }
    private Task OnCategoryChanged(CategoryDto? c) { _categoryFilter = c?.Id; _page = 1; return ReloadAsync(); }
    private Task OnAccountChanged(CashAccountDto? a) { _accountFilter = a?.Id; _page = 1; return ReloadAsync(); }

    private Task PrevPageAsync() { if (_page > 1) { _page--; return ReloadAsync(); } return Task.CompletedTask; }
    private Task NextPageAsync() { if (_page < _totalPages) { _page++; return ReloadAsync(); } return Task.CompletedTask; }

    private async Task ShowCreateAsync()
    {
        var model = new TransactionFormModel { OccurredOn = DateTime.UtcNow.Date };
        var result = await DialogService.ShowAsync<TransactionFormDialog, TransactionFormModel>(model, new BoDialogOptions
        {
            Title = "New transaction",
            Width = "560px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is TransactionFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess("Transaction recorded.");
            await RefreshAfterChangeAsync();
        }
    }

    private async Task ShowImportAsync()
    {
        var result = await DialogService.ShowAsync<ImportCashCounterDialog, CashImportModel>(new CashImportModel(), new BoDialogOptions
        {
            Title = "Import from Cash Counter",
            Width = "640px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is CashImportModel done && done.ImportedCount > 0)
        {
            ToastService.ShowSuccess($"Imported {done.ImportedCount} invoice{(done.ImportedCount == 1 ? "" : "s")} ({done.ImportedTotal:#,##0.00}).");
            if (done.SkippedMethods.Count > 0)
                ToastService.ShowWarning($"No cash account for: {string.Join(", ", done.SkippedMethods)} — skipped.");
            await RefreshAfterChangeAsync();
        }
    }

    private async Task ShowEditAsync(TransactionListItemDto t)
    {
        var model = new TransactionFormModel
        {
            Id = t.Id,
            OccurredOn = t.OccurredOn,
            Type = t.Type,
            CashAccountId = t.CashAccountId,
            CategoryId = t.CategoryId,
            Amount = t.Amount,
            Reference = t.Reference,
            Notes = t.Notes
        };
        var result = await DialogService.ShowAsync<TransactionFormDialog, TransactionFormModel>(model, new BoDialogOptions
        {
            Title = $"Edit · {t.Number}",
            Width = "560px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is TransactionFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess("Transaction updated.");
            await ReloadAsync();
        }
    }

    private async Task DeleteAsync(TransactionListItemDto t)
    {
        var ok = await DialogService.ConfirmAsync(
            "Delete transaction", $"Delete {t.Number}? This removes it from balances and totals.", "Delete", "Cancel", "danger");
        if (!ok) return;
        try
        {
            await Mediator.Send(new DeleteTransactionCommand(t.Id));
            ToastService.ShowSuccess($"{t.Number} deleted.");
            await RefreshAfterChangeAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }

    // After add/delete the row count changes; step back a page if the last row on the last page went away.
    private async Task RefreshAfterChangeAsync()
    {
        await ReloadAsync();
        if (_items.Count == 0 && _page > 1)
        {
            _page--;
            await ReloadAsync();
        }
    }
}
