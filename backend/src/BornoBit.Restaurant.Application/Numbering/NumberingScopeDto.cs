using BornoBit.Restaurant.Domain.Numbering;

namespace BornoBit.Restaurant.Application.Numbering;

public record NumberingScopeDto(
    Guid Id,
    string Code,
    string Name,
    string Prefix,
    NumberingCadence Cadence,
    byte Digits,
    bool ResetByOutlet,
    bool IsActive,
    DateTime CreatedAtUtc);
