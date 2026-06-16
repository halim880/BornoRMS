using BornoBit.Restaurant.Application.Accounting.Audit;
using BornoBit.Restaurant.Application.Accounting.Posting;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Transactions;

/// <summary>
/// Remits collected output VAT to the authority (NBR): Dr VAT Payable (2200) / Cr cash-leaf. Clears the
/// liability the cash-counter import accrued. Pure GL entry — no cash-book <see cref="FinanceTransaction"/>
/// is created here (the cash side is the GL cash-leaf credit).
/// </summary>
public record RecordVatPaymentCommand(Guid CashAccountId, DateTime PaidOn, decimal Amount, string? Reference)
    : IRequest<Guid>;

public class RecordVatPaymentCommandValidator : AbstractValidator<RecordVatPaymentCommand>
{
    public RecordVatPaymentCommandValidator()
    {
        RuleFor(x => x.CashAccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reference).MaximumLength(80);
    }
}

public class RecordVatPaymentCommandHandler : IRequestHandler<RecordVatPaymentCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly IGeneralLedgerService _gl;
    private readonly ICurrentUser _currentUser;

    public RecordVatPaymentCommandHandler(IAppDbContext db, IGeneralLedgerService gl, ICurrentUser currentUser)
    {
        _db = db;
        _gl = gl;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(RecordVatPaymentCommand request, CancellationToken cancellationToken)
    {
        var cashAccount = await _db.CashAccounts.FirstOrDefaultAsync(a => a.Id == request.CashAccountId && a.IsActive, cancellationToken)
            ?? throw new ValidationException("The selected cash account does not exist or is inactive.");

        var cashGl = await ChartOfAccountsMapper.EnsureCashAccountGlAsync(_db, cashAccount, cancellationToken);
        var entry = await _gl.PostAsync(_db, request.PaidOn, VoucherType.Payment, new[]
        {
            GlPostingLine.Dr(GlCodes.VatPayable, request.Amount, "VAT remittance"),
            GlPostingLine.CrId(cashGl, request.Amount, $"Paid from {cashAccount.Name}")
        }, reference: request.Reference ?? "VAT remittance", narration: "Output VAT remittance to authority", cancellationToken);

        FinancialAudit.Write(_db, FinancialAuditAction.CashImported, _currentUser, nameof(JournalEntry), entry.Id,
            amount: request.Amount, notes: $"VAT remittance {request.Reference}");

        await _db.SaveChangesAsync(cancellationToken);
        return entry.Id;
    }
}
