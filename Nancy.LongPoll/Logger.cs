using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nancy.LongPoll
{
  public interface ILogger
  {
    void LogException(Exception ex, string message = "");
    void LogError(string message);
    void LogDebug(string message);
    void LogWarn(string message);
    void LogInfo(string message);
    void LogTrace(string message);
  }
  public class EmptyLogger : ILogger
  {
    public void LogException(Exception ex, string message = "")
    {
    }

    public void LogError(string message = "")
    {
    }

    public void LogDebug(string message = "")
    {
    }

    public void LogWarn(string message = "")
    {
    }

    public void LogInfo(string message = "")
    {
    }

    public void LogTrace(string message = "")
    {
    }
  }
}
