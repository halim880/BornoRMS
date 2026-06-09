namespace BornoBit.Restaurant.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
