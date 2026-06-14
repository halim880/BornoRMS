using BornoBit.Restaurant.Domain.Dining;
using Xunit;

namespace BornoBit.Restaurant.Tests.Unit;

/// <summary>The table edit-hold that prevents two terminals grabbing the same dine-in table.</summary>
public class TableHoldTests
{
    private static readonly TimeSpan Hold = TimeSpan.FromMinutes(3);

    private static RestaurantTable NewTable() => RestaurantTable.Create("T1", 4);

    [Fact]
    public void Hold_blocks_another_user_within_the_window()
    {
        var now = DateTime.UtcNow;
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var table = NewTable();

        table.Hold(alice, "Alice", now, Hold);

        Assert.True(table.IsHeldByOther(bob, now.AddMinutes(1)));
        Assert.False(table.IsHeldByOther(alice, now.AddMinutes(1)));
    }

    [Fact]
    public void Second_terminal_acquiring_an_active_hold_throws()
    {
        var now = DateTime.UtcNow;
        var table = NewTable();
        table.Hold(Guid.NewGuid(), "Alice", now, Hold);

        Assert.Throws<InvalidOperationException>(() =>
            table.Hold(Guid.NewGuid(), "Bob", now.AddMinutes(1), Hold));
    }

    [Fact]
    public void Hold_expires_and_can_be_re_taken()
    {
        var now = DateTime.UtcNow;
        var bob = Guid.NewGuid();
        var table = NewTable();
        table.Hold(Guid.NewGuid(), "Alice", now, Hold);

        // 4 minutes later the 3-minute hold has lapsed.
        var later = now.AddMinutes(4);
        Assert.False(table.IsHeldByOther(bob, later));
        table.Hold(bob, "Bob", later, Hold); // does not throw
        Assert.True(table.IsHeldByOther(Guid.NewGuid(), later.AddMinutes(1)));
    }

    [Fact]
    public void Same_user_can_refresh_their_own_hold()
    {
        var now = DateTime.UtcNow;
        var alice = Guid.NewGuid();
        var table = NewTable();
        table.Hold(alice, "Alice", now, Hold);

        table.Hold(alice, "Alice", now.AddMinutes(1), Hold); // refresh, no throw

        Assert.True(table.IsHeldByOther(Guid.NewGuid(), now.AddMinutes(3)));
    }

    [Fact]
    public void Release_by_another_user_is_a_noop_while_active()
    {
        var now = DateTime.UtcNow;
        var alice = Guid.NewGuid();
        var table = NewTable();
        table.Hold(alice, "Alice", now, Hold);

        table.ReleaseHold(Guid.NewGuid(), now.AddMinutes(1)); // someone else cannot steal it

        Assert.True(table.IsHeldByOther(Guid.NewGuid(), now.AddMinutes(1)));
    }

    [Fact]
    public void Release_by_holder_clears_the_hold()
    {
        var now = DateTime.UtcNow;
        var alice = Guid.NewGuid();
        var table = NewTable();
        table.Hold(alice, "Alice", now, Hold);

        table.ReleaseHold(alice, now.AddMinutes(1));

        Assert.False(table.IsHeldByOther(Guid.NewGuid(), now.AddMinutes(1)));
    }
}
