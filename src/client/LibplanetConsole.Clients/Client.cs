using System.ComponentModel.Composition;
using System.Security;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using LibplanetConsole.Clients.Serializations;
using LibplanetConsole.Clients.Services;
using LibplanetConsole.Common;
using LibplanetConsole.Common.Extensions;
using LibplanetConsole.Nodes;
using LibplanetConsole.Nodes.Serializations;
using LibplanetConsole.Nodes.Services;

namespace LibplanetConsole.Clients;

[method: ImportingConstructor]
internal sealed class Client(ApplicationBase application, PrivateKey privateKey)
    : IClient, INodeCallback
{
    private readonly ApplicationBase _application = application;
    private readonly SecureString _privateKey = PrivateKeyUtility.ToSecureString(privateKey);
    private RemoteNodeContext? _remoteNodeContext;
    private Guid _closeToken;
    private ClientInfo _info = new() { Address = privateKey.PublicKey.Address };

    public event EventHandler<BlockEventArgs>? BlockAppended;

    public event EventHandler? Started;

    public event EventHandler<StopEventArgs>? Stopped;

    public PublicKey PublicKey { get; } = privateKey.PublicKey;

    public Address Address => PublicKey.Address;

    public TextWriter Out { get; set; } = Console.Out;

    public ClientInfo Info => _info;

    public NodeInfo NodeInfo { get; private set; } = new();

    public bool IsRunning { get; private set; }

    public ClientOptions ClientOptions { get; private set; } = new();

    private INodeService RemoteNodeService => _application.GetService<RemoteNodeService>().Service;

    public override string ToString() => $"[{Address}]";

    public bool Verify(object obj, byte[] signature)
        => PublicKeyUtility.Verify(PublicKey, obj, signature);

    public async Task StartAsync(ClientOptions clientOptions, CancellationToken cancellationToken)
    {
        if (_remoteNodeContext != null)
        {
            throw new InvalidOperationException("The client is already running.");
        }

        _remoteNodeContext = _application.GetService<RemoteNodeContext>();
        _remoteNodeContext.EndPoint = clientOptions.NodeEndPoint;
        _closeToken = await _remoteNodeContext.OpenAsync(cancellationToken);
        _remoteNodeContext.Closed += RemoteNodeContext_Closed;
        NodeInfo = await RemoteNodeService.GetInfoAsync(cancellationToken);
        _info = _info with { NodeAddress = NodeInfo.Address };
        ClientOptions = clientOptions;
        IsRunning = true;
        Started?.Invoke(this, EventArgs.Empty);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_remoteNodeContext == null)
        {
            throw new InvalidOperationException("The client is not running.");
        }

        _remoteNodeContext.Closed -= RemoteNodeContext_Closed;
        await _remoteNodeContext.CloseAsync(_closeToken, cancellationToken);
        _info = _info with { NodeAddress = default };
        _remoteNodeContext = null;
        _closeToken = Guid.Empty;
        ClientOptions = new();
        IsRunning = false;
        Stopped?.Invoke(this, new(StopReason.None));
    }

    public async Task<TxId> SendTransactionAsync(
        IAction[] actions, CancellationToken cancellationToken)
    {
        var privateKey = PrivateKeyUtility.FromSecureString(_privateKey);
        var address = privateKey.Address;
        var nonce = await RemoteNodeService.GetNextNonceAsync(address, cancellationToken);
        var genesisHash = NodeInfo.GenesisHash;
        var tx = Transaction.Create(
            nonce: nonce,
            privateKey: privateKey,
            genesisHash: genesisHash,
            actions: [.. actions.Select(item => item.PlainValue)]
        );
        return await RemoteNodeService.SendTransactionAsync(tx.Serialize(), cancellationToken);
    }

    public void InvokeNodeStartedEvent(NodeInfo nodeInfo)
    {
        NodeInfo = nodeInfo;
        _info = _info with { NodeAddress = NodeInfo.Address };
    }

    public void InvokeNodeStoppedEvent()
    {
        NodeInfo = new();
        _info = _info with { NodeAddress = default };
    }

    public void InvokeBlockAppendedEvent(BlockInfo blockInfo)
    {
        BlockAppended?.Invoke(this, new BlockEventArgs(blockInfo));
    }

    public async ValueTask DisposeAsync()
    {
        if (_remoteNodeContext != null)
        {
            _remoteNodeContext.Closed -= RemoteNodeContext_Closed;
            await _remoteNodeContext.CloseAsync(_closeToken);
            _remoteNodeContext = null;
        }
    }

    void INodeCallback.OnStarted(NodeInfo nodeInfo)
    {
        NodeInfo = nodeInfo;
    }

    void INodeCallback.OnStopped()
    {
        NodeInfo = new();
    }

    void INodeCallback.OnBlockAppended(BlockInfo blockInfo)
    {
        BlockAppended?.Invoke(this, new BlockEventArgs(blockInfo));
    }

    private void RemoteNodeContext_Closed(object? sender, EventArgs e)
    {
        if (_remoteNodeContext != null)
        {
            _remoteNodeContext.Closed -= RemoteNodeContext_Closed;
            _remoteNodeContext = null;
        }

        _closeToken = Guid.Empty;
        ClientOptions = new();
        IsRunning = false;
        Stopped?.Invoke(this, new(StopReason.None));
    }
}