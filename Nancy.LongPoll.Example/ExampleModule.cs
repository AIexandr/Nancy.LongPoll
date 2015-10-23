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
          return GetEmbeddedResource("Nancy.LongPoll.Example.Index.html");
        };

      Get["/poll.js"] = x =>
      {
        return GetEmbeddedResource("Nancy.LongPoll.poll.js", typeof(PollModule).Assembly);
      };
    }

    private string GetEmbeddedResource(string resourceName, Assembly assembly = null)
    {
      if (assembly == null) assembly = Assembly.GetExecutingAssembly();

      using (Stream stream = assembly.GetManifestResourceStream(resourceName))
      using (StreamReader reader = new StreamReader(stream))
      {
        string result = reader.ReadToEnd();

        return result;
      }
    }
  }
}
