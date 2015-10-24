using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nancy.LongPoll
{
  public interface ISessionProvider
  {
    string SessionId { get; }
  }

  public class DefaultSessionProvider : ISessionProvider
  {
    public static readonly string SessionIdCookieName = "nancy_long_poll_session_id";
    Request _Request = null;

    public DefaultSessionProvider()
    {
    }

    public DefaultSessionProvider(Request request)
    {
      if (request == null) throw new ArgumentNullException("request");

      _Request = request;
    }

    public string SessionId
    {
      get
      {
        string sessionId = null;
        if (!_Request.Cookies.ContainsKey(SessionIdCookieName))
        {
          sessionId = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        }
        else
        {
          sessionId = _Request.Cookies[SessionIdCookieName];
        }

        return sessionId;
      }
    }
  }
}
