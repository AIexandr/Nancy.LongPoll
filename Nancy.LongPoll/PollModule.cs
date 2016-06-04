using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Threading;
using Nancy;
using Newtonsoft.Json;

namespace Nancy.LongPoll
{
  public class PollModule : NancyModule
  {
    #region Nancy's module implementation
    ISessionProvider _SessionProvider = null;
    PollService _PollService = null;

    public PollModule(TinyIoc.TinyIoCContainer container, IPollService pollService = null, ISessionProvider sessionProvider = null)
    {
      if (container == null) throw new ArgumentNullException("container");
      if (!(sessionProvider is DefaultSessionProvider)) _SessionProvider = sessionProvider;

      if (pollService == null)
      {
        container.Register<IPollService, PollService>().AsSingleton();
        pollService = container.Resolve<IPollService>();
      }
      _PollService = pollService as PollService;
      if (pollService == null) throw new ApplicationException("Support Nany.LongPoll.PollService implementation only");

      Get["/Poll/Register"] = x =>
      {
        var sp = _SessionProvider;
        if (sp == null) sp = new DefaultSessionProvider(Request);

        var response = Response.AsJson(_PollService.Register(Request.UserHostAddress, sp.SessionId));
        if (sp is DefaultSessionProvider)
        {
          response = response.WithCookie(DefaultSessionProvider.SessionIdCookieName, sp.SessionId);
        }

        return response;
      };
      Get["/Poll/Wait"] = x =>
      {
        string clientId = Request.Query.clientId;
        ulong seqCode = Request.Query.seqCode;

        return Response.AsText(JsonConvert.SerializeObject(_PollService.Wait(clientId, seqCode)), "application/json");
      };
    }
    #endregion
  }
}
