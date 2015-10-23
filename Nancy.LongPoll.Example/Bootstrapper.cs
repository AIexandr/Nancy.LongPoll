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
    protected override void ApplicationStartup(TinyIoc.TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines)
    {
      base.ApplicationStartup(container, pipelines);

      ThreadPool.QueueUserWorkItem(new WaitCallback((o) =>
        {
          var counter = 0;
          while (true)
          {
            try
            {
              counter++;

              container.Resolve<PollService>().SendMessageToAllClients("TestMessage", 
                JsonConvert.SerializeObject(new 
                {
                  Counter = counter,
                  Message = "Hello Web Browser!",
                }));
            }
            catch (Exception ex)
            {
              // something wrong but no matters in the example 
            }
            Thread.Sleep(1000);
          }
        }));
    }

    protected override void ConfigureApplicationContainer(TinyIoc.TinyIoCContainer container)
    {
      base.ConfigureApplicationContainer(container);

      container.Register<PollService>().AsSingleton();
    }
  }
}
