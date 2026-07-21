using System;
using System.Security.Cryptography;
using System.Text;

namespace CareerOS.Api.Utils;

public static class PasswordHasher
{
    public static string HashPassword(string password)
    {
        // Simple and robust PBKDF2 hashing or SHA256 with a consistent salt for demo/dev purposes
        // Let's use SHA256 for maximum simplicity and performance in the sandbox, with a salt.
        using var sha = SHA256.Create();
        var salt = "CareerOS_Salt_Secret_Key_123!";
        var saltedBytes = Encoding.UTF8.GetBytes(password + salt);
        var hashBytes = sha.ComputeHash(saltedBytes);
        return Convert.ToBase64String(hashBytes);
    }

    public static bool VerifyPassword(string password, string hashedPassword)
    {
        return HashPassword(password) == hashedPassword;
    }
}
