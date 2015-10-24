using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nancy.LongPoll.Example
{
  public class ExampleNotificationService
  {
    PollService _PollService = null;
    public bool RunNotifications { get; set; }

    public ExampleNotificationService(PollService pollService)
    {
      if (pollService == null) throw new ArgumentNullException("pollService");

      _PollService = pollService;
      RunNotifications = true;

      ThreadPool.QueueUserWorkItem(new WaitCallback((o) =>
      {
        var counter = 0;
        while (true)
        {
          try
          {
            if (RunNotifications)
            {
              counter++;

              _PollService.SendMessageToAllClients("TestMessage",
                JsonConvert.SerializeObject(new
                {
                  Counter = counter,
                  Message = "Hello Web Browser!",
                }));
            }
          }
          catch (Exception ex)
          {
            // something wrong but no matters in the example 
          }
          Thread.Sleep(1000);
        }
      }));
    }
  }
}
