using CareerOS.Api.Utils;
using Xunit;

namespace CareerOS.Api.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_IsNonDeterministicAndNonEmpty()
    {
        var firstHash = PasswordHasher.HashPassword("CorrectHorseBatteryStaple!");
        var secondHash = PasswordHasher.HashPassword("CorrectHorseBatteryStaple!");

        Assert.False(string.IsNullOrWhiteSpace(firstHash));
        Assert.False(string.IsNullOrWhiteSpace(secondHash));
        Assert.NotEqual(firstHash, secondHash);
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
        var firstHash = PasswordHasher.HashPassword("CorrectHorseBatteryStaple!");
        var secondHash = PasswordHasher.HashPassword("CorrectHorseBatteryStaple!");

        Assert.True(PasswordHasher.VerifyPassword("CorrectHorseBatteryStaple!", firstHash));
        Assert.True(PasswordHasher.VerifyPassword("CorrectHorseBatteryStaple!", secondHash));
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForIncorrectPassword()
    {
        var hash = PasswordHasher.HashPassword("CorrectHorseBatteryStaple!");

        Assert.False(PasswordHasher.VerifyPassword("wrong-password", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid_format")]
    [InlineData("PBKDF2$notanumber$salt$hash")]
    [InlineData("PBKDF2$100000$notbase64!$notbase64!")]
    public void VerifyPassword_ReturnsFalseForInvalidHashFormat(string invalidHash)
    {
        Assert.False(PasswordHasher.VerifyPassword("CorrectHorseBatteryStaple!", invalidHash));
    }
}
