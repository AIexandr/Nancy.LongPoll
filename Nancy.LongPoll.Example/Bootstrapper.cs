using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nancy.LongPoll.Example
{
  public class Bootstrapper : DefaultNancyBootstrapper
  {
    protected override void ConfigureApplicationContainer(TinyIoc.TinyIoCContainer container)
    {
      base.ConfigureApplicationContainer(container);

      container.Register<PollService>().AsSingleton();
      container.Register<ExampleNotificationService>().AsSingleton();
    }
  }
}
