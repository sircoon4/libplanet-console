using System.Net;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using LibplanetConsole.Nodes;
using LibplanetConsole.Nodes.Serializations;

namespace LibplanetConsole.Consoles;

public interface INode : IAddressable, IAsyncDisposable, IServiceProvider
{
    event EventHandler<BlockEventArgs>? BlockAppended;

    event EventHandler? Started;

    event EventHandler? Stopped;

    event EventHandler? Disposed;

    bool IsRunning { get; }

    EndPoint EndPoint { get; }

    NodeInfo Info { get; }

    NodeOptions NodeOptions { get; }

    PublicKey PublicKey { get; }

    byte[] Sign(object obj);

    Task StartAsync(NodeOptions nodeOptions, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);

    Task<TxId> SendTransactionAsync(IAction[] actions, CancellationToken cancellationToken);

    Task<long> GetNextNonceAsync(Address address, CancellationToken cancellationToken);
}