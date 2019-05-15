using IPALogger = IPA.Logging.Logger;
using LogLevel = IPA.Logging.Logger.Level;

namespace CameraPlus
{
    internal static class Logger
    {
        internal static IPALogger log { private get; set; }

        internal static void Log(string message, LogLevel severity = LogLevel.Info) => log.Log(severity, message);
    }
}
