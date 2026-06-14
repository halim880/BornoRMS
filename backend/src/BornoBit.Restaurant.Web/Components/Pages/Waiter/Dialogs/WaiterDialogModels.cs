namespace BornoBit.Restaurant.Web.Components.Pages.Waiter.Dialogs;

public record TableOption(Guid Id, string Number, int Capacity);

public record GuestCountInput(string Prompt, int Guests);

public record PickTableInput(string Prompt, IReadOnlyList<TableOption> Tables);

public record MergeCandidate(Guid SessionId, string Table, decimal Bill, string Currency);
public record MergeInput(string SurvivorTable, IReadOnlyList<MergeCandidate> Candidates);

public record SplitOrderOption(Guid OrderId, string OrderNumber, string Status, decimal Total, string Currency);
public record SplitInput(IReadOnlyList<SplitOrderOption> Orders, IReadOnlyList<TableOption> Tables);
public record SplitResult(IReadOnlyList<Guid> OrderIds, Guid TargetTableId, int Guests);

public record StaffOption(Guid Id, string Name, string Roles);
public record TransferWaiterInput(string CurrentWaiter, IReadOnlyList<StaffOption> Staff);
public record TransferWaiterResult(Guid? WaiterUserId, string? WaiterName);
