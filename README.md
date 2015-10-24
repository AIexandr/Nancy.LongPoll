# Nancy.LongPoll
## What is it and what is it for at a glance
This is the long-polling (push, comet etc.) pattern implementation as a Nancy module.
See more about the Long Polling: https://en.wikipedia.org/wiki/Push_technology
See more about Nancy: http://nancyfx.org/

##Quick start
- Create an empty .Net project.
- Download and reference Nancy's dll's from Nuget or github: https://github.com/NancyFx
- Download the Nancy.LongPoll project from this repository and reference Nancy.LongPoll in your project.
- Create your custom bootstrapper derived from DefaultNancyBootstrapper class. Add PollService to the application IoC container *as a singleton*:

```C#
protected override void ConfigureApplicationContainer(TinyIoc.TinyIoCContainer container)
{
  base.ConfigureApplicationContainer(container);
  container.Register<PollService>().AsSingleton();
}
```
- Add a html to you project with linked poll.js. See Nancy's doc or the LongPoll example project how to make this.
- Somewhere in your html call startPoll().
- Override pollEvent(messageName, stringData) function to handle server events.
- Call SendMessageToAllClients(...) somewhere on the server. Of course you can send a message to a specific client or to a specific session . Every separate browser window (or a tab) is considered as a separate client but they can by grouped into sessions (cookie bases by default). You can override the default ISessionProvider implementation in the application or request IoC container.
- Done! 

For more detailed information on implementation please see the example project: https://github.com/AIexandr/Nancy.LongPoll/tree/master/Nancy.LongPoll.Example
