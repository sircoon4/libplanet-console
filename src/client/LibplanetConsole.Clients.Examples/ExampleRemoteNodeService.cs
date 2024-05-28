using System.ComponentModel.Composition;
using LibplanetConsole.Common.Services;
using LibplanetConsole.Examples.Services;

namespace LibplanetConsole.Clients.Examples;

[Export]
internal sealed class ExampleRemoteNodeService
    : RemoteService<IExampleNodeService, IExampleNodeCallbak>,
    IExampleNodeCallbak
{
    public void OnSubscribed(string address)
    {
    }

    public void OnUnsubscribed(string address)
    {
    }
}