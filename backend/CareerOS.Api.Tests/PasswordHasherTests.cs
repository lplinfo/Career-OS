using CareerOS.Api.Utils;
using Xunit;

namespace CareerOS.Api.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_IsDeterministicAndNonEmpty()
    {
        var firstHash = PasswordHasher.HashPassword("CorrectHorseBatteryStaple!");
        var secondHash = PasswordHasher.HashPassword("CorrectHorseBatteryStaple!");

        Assert.False(string.IsNullOrWhiteSpace(firstHash));
        Assert.Equal(firstHash, secondHash);
    }

    [Fact]
    public void HashPassword_DiffersForDifferentPasswords()
    {
        var firstHash = PasswordHasher.HashPassword("CorrectHorseBatteryStaple!");
        var secondHash = PasswordHasher.HashPassword("different-password");

        Assert.NotEqual(firstHash, secondHash);
    }

    [Fact]
    public void VerifyPassword_ReturnsTrueForCorrectPassword()
    {
        var hash = PasswordHasher.HashPassword("CorrectHorseBatteryStaple!");

        Assert.True(PasswordHasher.VerifyPassword("CorrectHorseBatteryStaple!", hash));
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForIncorrectPassword()
    {
        var hash = PasswordHasher.HashPassword("CorrectHorseBatteryStaple!");

        Assert.False(PasswordHasher.VerifyPassword("wrong-password", hash));
    }
}
