using System.Reflection;

namespace Logs.Models
{
    public static class VersionInfo
    {
        public static string Version =>
            Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0] ?? "Unknown Version";
    }
}
