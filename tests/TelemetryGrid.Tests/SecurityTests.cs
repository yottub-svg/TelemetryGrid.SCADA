using TelemetryGrid.Security;
using Xunit;

namespace TelemetryGrid.Tests;

public sealed class SecurityTests
{
    [Fact]
    public void Revoking_a_user_invalidates_the_previous_stamp()
    {
        var store = new InMemoryUserSecurityStore();
        var stamp = store.GetStamp("operator-1");

        Assert.True(store.IsValid("operator-1", stamp));
        store.Revoke("operator-1");
        Assert.False(store.IsValid("operator-1", stamp));
    }
}
