namespace Prismbar.Core.Security
{
    public interface ISecretProtector
    {
        string Protect(string plain);
        string? Unprotect(string protectedBase64);
    }
}

namespace Prismbar.Core.Time
{
    public interface IClock
    {
        DateTimeOffset Now { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
    }
}
