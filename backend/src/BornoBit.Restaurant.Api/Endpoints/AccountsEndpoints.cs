using BornoBit.Restaurant.Application.Accounting.Accounts;
using BornoBit.Restaurant.Application.Accounting.BankRec;
using BornoBit.Restaurant.Application.Accounting.CashAccounts;
using BornoBit.Restaurant.Application.Accounting.Categories;
using BornoBit.Restaurant.Application.Accounting.FixedAssets;
using BornoBit.Restaurant.Application.Accounting.Journals;
using BornoBit.Restaurant.Application.Accounting.Payroll;
using BornoBit.Restaurant.Application.Accounting.Periods;
using BornoBit.Restaurant.Application.Accounting.Reports;
using BornoBit.Restaurant.Application.Accounting.Transactions;
using BornoBit.Restaurant.Application.Inventory.Payables;
using BornoBit.Restaurant.Application.Inventory.Reports;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;

namespace BornoBit.Restaurant.Api.Endpoints;

/// <summary>
/// REST surface for the Flutter Accounts / Finance / General-Ledger screens — mirrors the Blazor staff
/// console Accounts pages (Components/Pages/Accounts/**.razor). Every route forwards to an existing
/// Application-layer handler (registered via AddApplication()). Mounted under the versioned group →
/// /api/v1/staff/accounts/*. Admin-only.
///
/// Date-range query params (from/to) are parsed as plain calendar dates, like ReportsEndpoints —
/// defaulting to today (UTC) when omitted. Reports are read-only; a handful of simple writes are exposed
/// (create transaction, cash account, category, journal entry). Skipped writes are noted inline.
/// </summary>
public static class AccountsEndpoints
{
    public static IEndpointRouteBuilder MapAccountsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/staff/accounts")
            .RequireCors("Frontends")
            .RequireAuthorization("Admin")
            .WithTags("Accounts");

        // ---------- transactions ----------
        group.MapGet("/transactions", (
            ISender sender, DateTime? from, DateTime? to, TransactionType? type,
            Guid? categoryId, Guid? cashAccountId, int? page, int? pageSize, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetTransactionsQuery(
                from, to, type, categoryId, cashAccountId, page is > 0 ? page.Value : 1,
                pageSize is > 0 ? pageSize.Value : 50), ct))));

        // Period summary cards (income / expense / net / by-category) for the transactions screen.
        group.MapGet("/transactions/summary", (ISender sender, DateTime? from, DateTime? to, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetFinanceSummaryQuery(from, to), ct))));

        group.MapPost("/transactions", (CreateTransactionRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var id = await sender.Send(new CreateTransactionCommand(
                    body.OccurredOn, body.Type, body.CashAccountId, body.CategoryId,
                    body.Amount, body.Reference, body.Notes), ct);
                return Results.Created($"/api/v1/staff/accounts/transactions/{id}", new { id });
            }));

        // ---------- cash accounts ----------
        group.MapGet("/cash-accounts", (ISender sender, bool? activeOnly, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetCashAccountsQuery(activeOnly ?? false), ct))));

        group.MapPost("/cash-accounts", (CreateCashAccountRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var id = await sender.Send(new CreateCashAccountCommand(body.Name, body.Kind, body.OpeningBalance), ct);
                return Results.Created($"/api/v1/staff/accounts/cash-accounts/{id}", new { id });
            }));

        // ---------- categories ----------
        group.MapGet("/categories", (ISender sender, TransactionType? type, bool? activeOnly, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetCategoriesQuery(type, activeOnly ?? false), ct))));

        group.MapPost("/categories", (CreateCategoryRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var id = await sender.Send(new CreateCategoryCommand(body.Name, body.Type), ct);
                return Results.Created($"/api/v1/staff/accounts/categories/{id}", new { id });
            }));

        // ---------- payables (accounts payable, in Inventory.Payables) ----------
        group.MapGet("/payables", (ISender sender, bool? outstandingOnly, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetPayablesQuery(outstandingOnly ?? false), ct))));

        group.MapGet("/payables/payments", (ISender sender, Guid? supplierId, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetSupplierPaymentsQuery(supplierId), ct))));

        // ---------- fiscal periods ----------
        group.MapGet("/periods", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetPeriodsQuery(), ct))));

        // ---------- fixed assets ----------
        group.MapGet("/fixed-assets", (ISender sender, bool? activeOnly, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetFixedAssetsQuery(activeOnly ?? false), ct))));

        group.MapGet("/fixed-assets/depreciation-schedule", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetDepreciationScheduleQuery(), ct))));

        // ---------- bank reconciliation ----------
        group.MapGet("/bank-rec", (ISender sender, Guid? cashAccountId, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetBankReconciliationsQuery(cashAccountId), ct))));

        group.MapGet("/bank-rec/transactions", (ISender sender, Guid cashAccountId, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetReconTransactionsQuery(cashAccountId), ct))));

        // ---------- payroll ----------
        group.MapGet("/payroll/employees", (ISender sender, bool? activeOnly, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetEmployeesQuery(activeOnly ?? false), ct))));

        group.MapGet("/payroll/runs", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetPayrollRunsQuery(), ct))));

        group.MapGet("/payroll/runs/{id:guid}", (Guid id, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetPayrollRunQuery(id), ct))));

        // ---------- reports (cash-basis + operational) ----------
        // Cash-basis Profit & Loss over a date range.
        group.MapGet("/reports/profit-loss", (ISender sender, DateTime? from, DateTime? to, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetProfitAndLossQuery(from, to), ct))));

        // Day-end ("day close") report for a single business day.
        group.MapGet("/reports/day-end", (ISender sender, DateTime? date, CancellationToken ct) =>
            Exec(async () =>
            {
                var d = DateOnly.FromDateTime(date?.Date ?? DateTime.UtcNow.Date);
                return Results.Ok(await sender.Send(new GetDayEndReportQuery(d), ct));
            }));

        // Operational food-cost % report (consumption-based, in Inventory.Reports).
        group.MapGet("/reports/food-cost", (ISender sender, DateTime? from, DateTime? to, CancellationToken ct) =>
            Exec(async () =>
            {
                var (f, t) = Window(from, to);
                return Results.Ok(await sender.Send(new GetFoodCostReportQuery(f, t), ct));
            }));

        // Output-VAT collected over a date range, grouped by rate.
        group.MapGet("/reports/vat", (ISender sender, DateTime? from, DateTime? to, CancellationToken ct) =>
            Exec(async () =>
            {
                var (f, t) = Window(from, to);
                return Results.Ok(await sender.Send(new GetVatReportQuery(f, t), ct));
            }));

        // VAT remittance screen: live VAT Payable GL balance (the amount owed to the authority).
        group.MapGet("/reports/vat-remittance", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(
                new GetGlAccountBalanceQuery(Application.Accounting.Posting.GlCodes.VatPayable), ct))));

        // Combined cash book over a date range (CashAccountId left null = all accounts).
        group.MapGet("/reports/cash-book", (ISender sender, DateTime? from, DateTime? to, Guid? cashAccountId, CancellationToken ct) =>
            Exec(async () =>
            {
                var (f, t) = Window(from, to);
                return Results.Ok(await sender.Send(new GetCashLedgerQuery(f, t, cashAccountId), ct));
            }));

        // Single cash-account ledger (same query, requires a cashAccountId).
        group.MapGet("/reports/ledger", (ISender sender, DateTime? from, DateTime? to, Guid? cashAccountId, CancellationToken ct) =>
            Exec(async () =>
            {
                var (f, t) = Window(from, to);
                return Results.Ok(await sender.Send(new GetCashLedgerQuery(f, t, cashAccountId), ct));
            }));

        // ---------- general ledger ----------
        // Chart of accounts as a tree (roots → children).
        group.MapGet("/gl/chart", (ISender sender, bool? activeOnly, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetChartOfAccountsTreeQuery(activeOnly ?? false), ct))));

        // Flat list of accounts (for journal-line pickers).
        group.MapGet("/gl/accounts", (ISender sender, bool? postableOnly, bool? activeOnly, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetAccountsQuery(postableOnly ?? false, activeOnly ?? false), ct))));

        // Paged journal entries, newest first.
        group.MapGet("/gl/journal", (
            ISender sender, DateTime? from, DateTime? to, VoucherType? voucherType,
            JournalStatus? status, int? page, int? pageSize, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetJournalEntriesQuery(
                from, to, voucherType, status, page is > 0 ? page.Value : 1,
                pageSize is > 0 ? pageSize.Value : 50), ct))));

        group.MapGet("/gl/journal/{id:guid}", (Guid id, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetJournalEntryQuery(id), ct))));

        group.MapPost("/gl/journal", (CreateJournalEntryRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var lines = (body.Lines ?? new List<JournalLineRequest>())
                    .Select(l => new JournalLineInput(l.AccountId, l.Debit, l.Credit, l.Narration))
                    .ToList();
                var result = await sender.Send(new CreateJournalEntryCommand(
                    body.EntryDate, body.VoucherType, body.Reference, body.Narration, lines, body.PostImmediately), ct);
                return Results.Created($"/api/v1/staff/accounts/gl/journal/{result.Id}", result);
            }));

        // Trial balance over Posted journal lines (optional date range).
        group.MapGet("/gl/trial-balance", (ISender sender, DateTime? from, DateTime? to, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetTrialBalanceQuery(from, to), ct))));

        // GL-derived Profit & Loss (from posted journal lines).
        group.MapGet("/gl/profit-loss", (ISender sender, DateTime? from, DateTime? to, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetGlProfitAndLossQuery(from, to), ct))));

        // GL-derived Balance Sheet as of a date.
        group.MapGet("/gl/balance-sheet", (ISender sender, DateTime? asOf, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetBalanceSheetQuery(asOf), ct))));

        return app;
    }

    private static (DateTime From, DateTime To) Window(DateTime? from, DateTime? to)
    {
        var today = DateTime.UtcNow.Date;
        return (from?.Date ?? today, to?.Date ?? today);
    }

    // Shared error translation so FluentValidation failures surface as 400, not 500.
    private static async Task<IResult> Exec(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new { message = errors.FirstOrDefault() ?? "Validation failed.", errors });
        }
        catch (NotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
    }

    // ---------- request bodies ----------
    public record CreateTransactionRequest(
        DateTime OccurredOn, TransactionType Type, Guid CashAccountId, Guid CategoryId,
        decimal Amount, string? Reference, string? Notes);

    public record CreateCashAccountRequest(string Name, CashAccountKind Kind, decimal OpeningBalance);

    public record CreateCategoryRequest(string Name, TransactionType Type);

    public record JournalLineRequest(Guid AccountId, decimal Debit, decimal Credit, string? Narration);

    public record CreateJournalEntryRequest(
        DateTime EntryDate, VoucherType VoucherType, string? Reference, string? Narration,
        List<JournalLineRequest>? Lines, bool PostImmediately = false);
}
