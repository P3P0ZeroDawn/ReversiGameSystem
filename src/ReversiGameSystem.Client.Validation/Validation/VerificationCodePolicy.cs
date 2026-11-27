using System.Linq;

namespace ReversiGameSystem.Validation;

public static class VerificationCodePolicy
{
    public const int Length = 6;

    public static bool IsValid(string? code)
    {
        if (code == null || code.Length != Length)
        {
            return false;
        }
        return code.All(digit => digit >= '0' && digit <= '9');
    }
}
