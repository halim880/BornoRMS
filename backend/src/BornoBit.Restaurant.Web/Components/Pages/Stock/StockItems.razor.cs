using System.Web;
using BornoBit.Restaurant.Application.Inventory.Categories;
using BornoBit.Restaurant.Application.Inventory.Items;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BornoBit.Restaurant.Web.Components.Pages.Stock;

public partial class StockItems : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<InventoryItemDto> _items = new();
    private InventoryStockSummaryDto _summary = new(0, 0m, 0);

    private List<InventoryCategoryDto> _categories = new();

    // Filters
    private string? _search;
    private Guid? _categoryFilter;
    private InventoryItemType? _typeFilter;
    private bool _lowStockOnly;
    private bool _showInactive = true;

    // Sort
    private string? _sortBy;     // null = default category/name ordering
    private bool _sortDesc;

    // Paging
    private int _page = 1;
    private int _pageSize = 50;
    private int _totalPages = 1;
    private long _totalCount;

    public record TypeOption(string Key, string Label, InventoryItemType? Value);
    private readonly List<TypeOption> _typeOptions = new()
    {
        new("", "All types", null),
        new("Ingredient", "Ingredient", InventoryItemType.Ingredient),
        new("FinishedGood", "Finished", InventoryItemType.FinishedGood),
    };
    private TypeOption _selectedType = null!;

    public record PageSizeOption(int Value, string Label);
    private readonly List<PageSizeOption> _pageSizeOptions = new()
    {
        new(25, "25 / page"),
        new(50, "50 / page"),
        new(100, "100 / page"),
    };
    private PageSizeOption _selectedPageSize = null!;

    private InventoryCategoryDto? _selectedCategory => _categories.FirstOrDefault(c => c.Id == _categoryFilter);

    private bool _hasFilters => !string.IsNullOrWhiteSpace(_search) || _categoryFilter is not null
        || _typeFilter is not null || _lowStockOnly || !_showInactive;

    protected override async Task OnInitializedAsync()
    {
        _selectedType = _typeOptions[0];
        _selectedPageSize = _pageSizeOptions[1];
        try { _categories = (await Mediator.Send(new GetInventoryCategoriesQuery())).ToList(); }
        catch (Exception ex) { _error = $"Failed to load categories: {ex.Message}"; }
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            var result = await Mediator.Send(new GetInventoryItemsQuery(
                Search: _search,
                CategoryId: _categoryFilter,
                ItemType: _typeFilter,
                LowStockOnly: _lowStockOnly,
                IncludeInactive: _showInactive,
                SortBy: _sortBy,
                SortDesc: _sortDesc,
                Page: _page,
                PageSize: _pageSize));
            _items = result.Items.ToList();
            _totalPages = Math.Max(1, result.TotalPages);
            _totalCount = result.TotalCount;

            _summary = await Mediator.Send(new GetInventoryStockSummaryQuery(
                Search: _search,
                CategoryId: _categoryFilter,
                ItemType: _typeFilter,
                LowStockOnly: _lowStockOnly,
                IncludeInactive: _showInactive));
        }
        catch (Exception ex) { _error = $"Failed to load stock items: {ex.Message}"; }
        finally { _loading = false; }
    }

    private Task OnSearchChanged(string? v) { _search = v; _page = 1; return ReloadAsync(); }
    private Task OnCategoryChanged(InventoryCategoryDto? c) { _categoryFilter = c?.Id; _page = 1; return ReloadAsync(); }
    private Task OnTypeChanged(TypeOption? o) { _selectedType = o ?? _typeOptions[0]; _typeFilter = _selectedType.Value; _page = 1; return ReloadAsync(); }
    private Task OnLowStockChanged(bool v) { _lowStockOnly = v; _page = 1; return ReloadAsync(); }
    private Task OnShowInactiveChanged(bool v) { _showInactive = v; _page = 1; return ReloadAsync(); }
    private Task OnPageSizeChanged(PageSizeOption? o) { _selectedPageSize = o ?? _pageSizeOptions[1]; _pageSize = _selectedPageSize.Value; _page = 1; return ReloadAsync(); }

    private Task SortByAsync(string column)
    {
        if (_sortBy == column) _sortDesc = !_sortDesc;
        else { _sortBy = column; _sortDesc = false; }
        _page = 1;
        return ReloadAsync();
    }

    private string SortIndicator(string column) => _sortBy == column ? (_sortDesc ? " ▼" : " ▲") : "";

    private Task PrevPageAsync() { if (_page > 1) { _page--; return ReloadAsync(); } return Task.CompletedTask; }
    private Task NextPageAsync() { if (_page < _totalPages) { _page++; return ReloadAsync(); } return Task.CompletedTask; }

    private async Task PrintPdfAsync()
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(_search)) q["search"] = _search;
        if (_categoryFilter is { } cid) q["categoryId"] = cid.ToString();
        if (_typeFilter is { } it) q["itemType"] = it.ToString();
        if (_lowStockOnly) q["lowStockOnly"] = "true";
        if (_showInactive) q["includeInactive"] = "true";
        if (!string.IsNullOrEmpty(_sortBy)) { q["sortBy"] = _sortBy; q["sortDesc"] = _sortDesc ? "true" : "false"; }

        var qs = q.ToString();
        var url = Nav.ToAbsoluteUri("/reports/stock/valuation.pdf" + (string.IsNullOrEmpty(qs) ? "" : "?" + qs)).ToString();
        await JS.InvokeVoidAsync("open", url, "_blank");
    }

    private async Task ShowCreateAsync()
    {
        var model = new StockItemFormModel();
        var result = await DialogService.ShowAsync<StockItemFormDialog, StockItemFormModel>(model, new BoDialogOptions
        {
            Title = "New stock item",
            Width = "640px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is StockItemFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess($"Stock item '{saved.Name}' created.");
            await ReloadAsync();
        }
    }

    private async Task ShowEditAsync(InventoryItemDto i)
    {
        var model = new StockItemFormModel
        {
            Id = i.Id,
            Code = i.Code,
            Name = i.Name,
            BanglaName = i.BanglaName,
            InventoryCategoryId = i.InventoryCategoryId,
            ItemType = i.ItemType,
            BaseUnitId = i.BaseUnitId,
            ReorderLevel = i.ReorderLevel,
            ReorderQty = i.ReorderQty,
            IsPerishable = i.IsPerishable,
            PackSize = i.PackSize,
            PackNote = i.PackNote
        };
        var result = await DialogService.ShowAsync<StockItemFormDialog, StockItemFormModel>(model, new BoDialogOptions
        {
            Title = $"Edit · {i.Name}",
            Width = "640px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is StockItemFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess("Stock item updated.");
            await ReloadAsync();
        }
    }

    private async Task ShowAdjustAsync(InventoryItemDto i)
    {
        var model = new StockAdjustModel
        {
            ItemId = i.Id,
            ItemName = i.Name,
            UnitCode = i.UnitCode,
            CurrentQty = i.QtyOnHand,
            CountedQty = i.QtyOnHand
        };
        var result = await DialogService.ShowAsync<StockAdjustDialog, StockAdjustModel>(model, new BoDialogOptions
        {
            Title = $"Adjust stock · {i.Name}",
            Width = "480px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is StockAdjustModel saved && saved.Saved)
        {
            ToastService.ShowSuccess($"Stock adjusted for '{i.Name}'.");
            await ReloadAsync();
        }
    }

    private async Task ToggleActiveAsync(InventoryItemDto i, bool active)
    {
        try
        {
            await Mediator.Send(new SetInventoryItemActiveCommand(i.Id, active));
            ToastService.ShowSuccess($"'{i.Name}' {(active ? "activated" : "deactivated")}.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }
}
