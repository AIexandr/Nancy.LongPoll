using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nancy.LongPoll
{
  public interface IPollAccessControlService
  {
    bool AllowSendMessage(string sessionId, string messageName, string message);
  }

  public class EmptyAccessControlService : IPollAccessControlService
  {
    public bool AllowSendMessage(string sessionId, string messageName, string message)
    {
      return true;
    }
  }
}
