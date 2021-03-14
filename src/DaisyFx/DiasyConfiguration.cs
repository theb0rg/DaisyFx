using DaisyFx.Hosting;
using System;

namespace DaisyFx
{
    public class DaisyConfiguration : IDaisyConfiguration
    {
        public string HostMode { get; }
        private const string DefaultHostMode = ServiceHostInterface.Mode;

        public DaisyConfiguration(string? hostMode)
        {
            HostMode = GetHostMode(hostMode);

            if (!IsValidHostMode(HostMode)) throw new ArgumentException($"{HostMode} is not a valid value.", nameof(HostMode));
        }

        private static string GetHostMode(string? hostMode) => string.IsNullOrWhiteSpace(hostMode) ? DefaultHostMode : hostMode.ToLowerInvariant();
        private static bool IsValidHostMode(string hostMode) => hostMode switch
        {
            ConsoleHostInterface.Mode => true,
            ServiceHostInterface.Mode => true,
            _ => false
        };
    }
}