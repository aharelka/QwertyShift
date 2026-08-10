using System;

namespace QwertyShift
{
    public interface ILogger
    {
        void LogError(Exception ex, string context);
    }
}