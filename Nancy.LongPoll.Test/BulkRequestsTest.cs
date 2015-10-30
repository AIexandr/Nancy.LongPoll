using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nancy.Hosting.Self;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nancy.LongPoll.Test
{
  [TestClass]
  public class BulkRequestsTest
  {
    [TestMethod]
    public void TestBulkRequests()
    {
      var url = "http://127.0.0.1";

      var host = new NancyHost(new NancyTestBootstrapper(), new HostConfiguration
      {
        UrlReservations = new UrlReservations { CreateAutomatically = true }
      }, new Uri(url));
      host.Start();

      //Process.Start(url + "/TestBulkRequests/Diag");
      Process.Start(url + "/TestBulkRequests/TestBulkRequests.html");
       
      while (!StopTest)
      {
        Thread.Sleep(1000);
      }
    }

    public static bool StopTest { get; set; }
    public static bool StopWait { get; set; }
  }

  public class NancyTestBootstrapper : DefaultNancyBootstrapper
  {
    protected override void ApplicationStartup(TinyIoc.TinyIoCContainer container, Bootstrapper.IPipelines pipelines)
    {
      base.ApplicationStartup(container, pipelines);
    }

    protected override Diagnostics.DiagnosticsConfiguration DiagnosticsConfiguration
    {
      get
      {
        var config = base.DiagnosticsConfiguration;
        config.Password = "123";
        config.Path = "/TestBulkRequests/Diag";

        return config;
      }
    }
  }

  public class TestBulkRequestsModule : NancyModule
  {
    static int _Counter = 0;

    public TestBulkRequestsModule()
    {
      Get["/TestBulkRequests/{file}"] = x =>
        {
          return ContentModule.GetEmbeddedResource("Nancy.LongPoll.Test." + x.file, typeof(TestBulkRequestsModule).Assembly);
        };

      Get["/TestBulkRequests/StopTest"] = x =>
        {
          BulkRequestsTest.StopTest = true;

          return "";
        };

      Get["/TestBulkRequests/StopWait"] = x =>
      {
        BulkRequestsTest.StopWait = true;

        return "";
      };

      Get["/TestBulkRequests/Wait"] = x =>
        {
          Interlocked.Increment(ref _Counter);

          while (!BulkRequestsTest.StopWait)
          {
            Thread.Sleep(1000);
          }

          return "";
        };

      Get["/TestBulkRequests/RequestsCount"] = x =>
        {
          return _Counter.ToString();
        };
    }
  }
}
