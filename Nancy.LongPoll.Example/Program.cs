using Nancy.Hosting.Self;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nancy.LongPoll.Example
{
  class Program
  {
    static void Main(string[] args)
    {
      var url = "http://127.0.0.1";

      var host = new NancyHost(new HostConfiguration
      {
        UrlReservations = new UrlReservations { CreateAutomatically = true }
      }, new Uri(url));
      host.Start();

      Process.Start(url);
      Console.WriteLine("Press the Enter key for exit...");
      Console.ReadLine();
    }
  }
}
