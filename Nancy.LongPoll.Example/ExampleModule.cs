using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nancy.LongPoll.Example
{
  public class ExampleModule : NancyModule
  {
    ExampleNotificationService _ExampleNotificationService = null;

    public ExampleModule(ExampleNotificationService exampleNotificationService)
    {
      if (exampleNotificationService == null) throw new ArgumentNullException("exampleNotificationService");

      _ExampleNotificationService = exampleNotificationService;

      Get["/"] = x =>
      {
        return ContentModule.GetEmbeddedResource("Nancy.LongPoll.Example.Index.html", Assembly.GetExecutingAssembly());
      };

      Post["/Stop"] = x =>
      {
        _ExampleNotificationService.RunNotifications = false;

        return null;
      };

      Post["/Start"] = x =>
      {
        _ExampleNotificationService.RunNotifications = true;

        return null;
      };
    }
  }
}
