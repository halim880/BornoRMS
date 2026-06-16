using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Payroll;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Payroll;

public record EmployeeDto(
    Guid Id, string Code, string FullName, string? Designation, decimal BaseSalary,
    decimal OvertimeRate, EmployeeStatus Status, DateTime JoinedOn);

public record CreateEmployeeCommand(
    string Code, string FullName, string? Designation, decimal BaseSalary, decimal OvertimeRate, DateTime JoinedOn)
    : IRequest<Guid>;

public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Designation).MaximumLength(120);
        RuleFor(x => x.BaseSalary).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OvertimeRate).GreaterThanOrEqualTo(0);
    }
}

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Guid>
{
    private readonly IAppDbContext _db;
    public CreateEmployeeCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Employees.AnyAsync(e => e.Code == code, cancellationToken))
            throw new ConflictException($"An employee with code '{code}' already exists.");

        var employee = Employee.Create(request.Code, request.FullName, request.Designation,
            request.BaseSalary, request.OvertimeRate, request.JoinedOn);
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync(cancellationToken);
        return employee.Id;
    }
}

public record UpdateEmployeeCommand(
    Guid Id, string FullName, string? Designation, decimal BaseSalary, decimal OvertimeRate, EmployeeStatus Status)
    : IRequest<Unit>;

public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BaseSalary).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OvertimeRate).GreaterThanOrEqualTo(0);
    }
}

public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Unit>
{
    private readonly IAppDbContext _db;
    public UpdateEmployeeCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Employee not found.");
        employee.UpdateDetails(request.FullName, request.Designation, request.BaseSalary, request.OvertimeRate);
        employee.SetStatus(request.Status);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public record GetEmployeesQuery(bool ActiveOnly = false) : IRequest<IReadOnlyList<EmployeeDto>>;

public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, IReadOnlyList<EmployeeDto>>
{
    private readonly IAppDbContext _db;
    public GetEmployeesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Employees.AsQueryable();
        if (request.ActiveOnly) query = query.Where(e => e.Status == EmployeeStatus.Active);

        return await query
            .OrderBy(e => e.Code)
            .Select(e => new EmployeeDto(e.Id, e.Code, e.FullName, e.Designation, e.BaseSalary, e.OvertimeRate, e.Status, e.JoinedOn))
            .ToListAsync(cancellationToken);
    }
}
