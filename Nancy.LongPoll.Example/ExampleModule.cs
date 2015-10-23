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
    public ExampleModule()
    {
      Get["/"] = x =>
      {
        return ContentModule.GetEmbeddedResource("Nancy.LongPoll.Example.Index.html", Assembly.GetExecutingAssembly());
      };
    }
  }
}
