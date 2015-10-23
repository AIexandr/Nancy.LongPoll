using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nancy;
using Nancy.Testing;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nancy.LongPoll.Test
{
  [TestClass]
  public class PollModuleTest
  {
    [TestMethod]
    public void Register()
    {
      var bootstrapper = new DefaultNancyBootstrapper();
      var browser = new Browser(bootstrapper);

      var response = browser.Get("http://127.0.0.1/Poll/Register");
      var obj = JObject.Parse(response.Body.AsString()).ToObject<dynamic>();

      Assert.IsNotNull(obj);
      Assert.IsNotNull(obj.success);
      Assert.IsNotNull(obj.retcode);
      Assert.IsNotNull(obj.messageName);
      Assert.IsNotNull(obj.data);

      Assert.AreEqual(true, obj.success.Value);
      Assert.AreEqual("Ok", obj.retcode.Value);
      Assert.AreEqual(null, obj.messageName.Value);
      Assert.IsTrue(obj.data.Value is string);
    }

    [TestMethod]
    public void WaitClientNotRegistered()
    {
      var bootstrapper = new DefaultNancyBootstrapper();
      var browser = new Browser(bootstrapper);

      var response = browser.Get(string.Format("http://127.0.0.1/Poll/Wait?clientId={0}&seqCode={1}", Guid.NewGuid().ToString(), 0));
      Assert.IsNotNull(response);
      var responseAsString = response.Body.AsString();
      Assert.IsNotNull(responseAsString);
      Assert.IsTrue(responseAsString.Contains("ClientNotRegistered"));
    }

    [TestMethod]
    public void CheckCookie()
    {
      var bootstrapper = new CustomBootstrapper();
      var browser = new Browser(bootstrapper);

      var response = browser.Get("http://127.0.0.1/Poll/Register");
      var obj = JObject.Parse(response.Body.AsString()).ToObject<dynamic>();

      var sessionCookie = response.Cookies.FirstOrDefault(x => x.Name == "nancy_long_poll_session_id");
      Assert.IsNotNull(sessionCookie);

      var sessionId = sessionCookie.Value;
      Assert.IsFalse(string.IsNullOrWhiteSpace(sessionId));

      response = browser.Get("http://127.0.0.1/Poll/Register");
      obj = JObject.Parse(response.Body.AsString()).ToObject<dynamic>();

      sessionCookie = response.Cookies.FirstOrDefault(x => x.Name == "nancy_long_poll_session_id");
      Assert.IsNotNull(sessionCookie);
      var sessionId2 = sessionCookie.Value;
      Assert.IsFalse(string.IsNullOrWhiteSpace(sessionId));
      Assert.AreEqual(sessionId, sessionId2);
    }

    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
      protected override void ConfigureApplicationContainer(TinyIoc.TinyIoCContainer container)
      {
        base.ConfigureApplicationContainer(container);
      }
    }
  }
}
