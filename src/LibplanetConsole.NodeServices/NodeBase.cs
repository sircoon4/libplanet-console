using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Renderers;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Net.Consensus;
using Libplanet.Net.Options;
using Libplanet.Net.Transports;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using LibplanetConsole.Common;
using LibplanetConsole.Common.Exceptions;
using LibplanetConsole.NodeServices.Seeds;
using LibplanetConsole.NodeServices.Serializations;

namespace LibplanetConsole.NodeServices;

public abstract class NodeBase(PrivateKey privateKey) : IAsyncDisposable, IActionRenderer
{
    private readonly PrivateKey _privateKey = privateKey;
    private readonly SynchronizationContext _synchronizationContext
        = SynchronizationContext.Current!;

    private readonly PrivateKey _seedNodePrivateKey = new();
    private readonly ConcurrentDictionary<TxId, ManualResetEvent> _eventByTxId = [];
    private readonly ConcurrentDictionary<IValue, Exception> _exceptionByAction = [];

    private DnsEndPoint? _blocksyncEndPoint;
    private DnsEndPoint? _consensusEndPoint;
    private Swarm? _swarm;
    private Task? _startTask;
    private bool _isDisposed;
    private SeedNode? _blocksyncSeedNode;
    private SeedNode? _consensusSeedNode;

    public event EventHandler<BlockEventArgs>? BlockAppended;

    public event EventHandler? Started;

    public event EventHandler? Stopped;

    public DnsEndPoint SwarmEndPoint
    {
        get => _blocksyncEndPoint ?? throw new InvalidOperationException();
        set => _blocksyncEndPoint = value;
    }

    public DnsEndPoint ConsensusEndPoint
    {
        get => _consensusEndPoint ?? throw new InvalidOperationException();
        set => _consensusEndPoint = value;
    }

    public AppProtocolVersion AppProtocolVersion
        => BlockChainUtility.AppProtocolVersion;

    public bool IsRunning => _startTask != null;

    public bool IsDisposed => _isDisposed;

    public Address Address => _privateKey.Address;

    public PublicKey PublicKey => _privateKey.PublicKey;

    public PrivateKey PrivateKey => _privateKey;

    public BlockChain BlockChain => _swarm?.BlockChain ?? throw new InvalidOperationException();

    public NodeInfo Info => new(this);

    public BoundPeer[] Peers
    {
        get
        {
            if (_swarm != null)
            {
                return [.. _swarm.Peers];
            }

            throw new InvalidOperationException();
        }
    }

    public BoundPeer BlocksyncSeedPeer
        => _blocksyncSeedNode?.BoundPeer ?? NodeOptions.BlocksyncSeedPeer ??
            throw new InvalidOperationException();

    public BoundPeer ConsensusSeedPeer
        => _consensusSeedNode?.BoundPeer ?? NodeOptions.ConsensusSeedPeer ??
            throw new InvalidOperationException();

    public NodeOptions NodeOptions { get; private set; } = new();

    public override string ToString() => AddressUtility.ToString(Address);

    public Task<TxId> AddTransactionAsync(IAction[] actions, CancellationToken cancellationToken)
        => AddTransactionAsync(_privateKey, actions, cancellationToken);

    public async Task<TxId> AddTransactionAsync(
        PrivateKey privateKey, IAction[] actions, CancellationToken cancellationToken)
    {
        ObjectDisposedExceptionUtility.ThrowIf(_isDisposed, this);
        InvalidOperationExceptionUtility.ThrowIf(
            condition: _startTask == null || _swarm == null,
            message: "Swarm has been stopped.");

        var blockChain = BlockChain;
        var genesisBlock = blockChain.Genesis;
        var nonce = blockChain.GetNextTxNonce(privateKey.Address);
        var values = actions.Select(item => item.PlainValue).ToArray();
        var transaction = Transaction.Create(
            nonce: nonce,
            privateKey: privateKey,
            genesisHash: genesisBlock.Hash,
            actions: new TxActionList(values)
        );
        await AddTransactionAsync(transaction, cancellationToken);
        return transaction.Id;
    }

    public async Task StartAsync(NodeOptions nodeOptions, CancellationToken cancellationToken)
    {
        ObjectDisposedExceptionUtility.ThrowIf(_isDisposed, this);
        InvalidOperationExceptionUtility.ThrowIf(_startTask != null, "Swarm has been started.");

        var privateKey = _privateKey;
        var blocksyncEndPoint = _blocksyncEndPoint ?? DnsEndPointUtility.Next();
        var consensusEndPoint = _consensusEndPoint ?? DnsEndPointUtility.Next();
        var blocksyncSeedPeer = nodeOptions.BlocksyncSeedPeer ??
            new BoundPeer(_seedNodePrivateKey.PublicKey, DnsEndPointUtility.Next());
        var consensusSeedPeer = nodeOptions.ConsensusSeedPeer ??
            new BoundPeer(_seedNodePrivateKey.PublicKey, DnsEndPointUtility.Next());
        var swarmTransport
            = await CreateTransport(privateKey, blocksyncEndPoint, cancellationToken);
        var swarmOptions = new SwarmOptions
        {
            StaticPeers = ImmutableHashSet.Create(blocksyncSeedPeer),
        };
        var consensusTransport = await CreateTransport(
            privateKey: privateKey,
            endPoint: consensusEndPoint,
            cancellationToken: cancellationToken);
        var consensusReactorOption = new ConsensusReactorOption
        {
            SeedPeers = [consensusSeedPeer],
            ConsensusPort = consensusEndPoint.Port,
            ConsensusPrivateKey = privateKey,
            TargetBlockInterval = TimeSpan.FromSeconds(2),
            ContextTimeoutOptions = new(),
        };
        var blockChain = BlockChainUtility.CreateBlockChain(
            genesisOptions: nodeOptions.GenesisOptions,
            storePath: string.Empty,
            renderer: this);

        if (nodeOptions.BlocksyncSeedPeer is null)
        {
            _blocksyncSeedNode = new SeedNode(new()
            {
                PrivateKey = _seedNodePrivateKey,
                EndPoint = blocksyncSeedPeer.EndPoint,
                AppProtocolVersion = AppProtocolVersion,
            });
            await _blocksyncSeedNode.StartAsync(cancellationToken);
        }

        if (nodeOptions.ConsensusSeedPeer is null)
        {
            _consensusSeedNode = new SeedNode(new()
            {
                PrivateKey = _seedNodePrivateKey,
                EndPoint = consensusSeedPeer.EndPoint,
                AppProtocolVersion = AppProtocolVersion,
            });
            await _consensusSeedNode.StartAsync(cancellationToken);
        }

        _swarm = new Swarm(
            blockChain: blockChain,
            privateKey: privateKey,
            transport: swarmTransport,
            options: swarmOptions,
            consensusTransport: consensusTransport,
            consensusOption: consensusReactorOption);
        _startTask = _swarm.StartAsync(cancellationToken: default);
        _blocksyncEndPoint = blocksyncEndPoint;
        _consensusEndPoint = consensusEndPoint;
        NodeOptions = nodeOptions;
        Started?.Invoke(this, EventArgs.Empty);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedExceptionUtility.ThrowIf(_isDisposed, this);
        InvalidOperationExceptionUtility.ThrowIf(
            condition: _startTask == null || _swarm == null,
            message: "Swarm has been stopped.");

        NodeOptions = new();
        _blocksyncEndPoint = null;
        _consensusEndPoint = null;
        await _swarm!.StopAsync(cancellationToken: cancellationToken);
        await _startTask!;
        _swarm.Dispose();
        _swarm = null;
        _startTask = null;
        Stopped?.Invoke(this, EventArgs.Empty);
    }

    public async Task AddTransactionAsync(
        Transaction transaction, CancellationToken cancellationToken)
    {
        ObjectDisposedExceptionUtility.ThrowIf(_isDisposed, this);
        InvalidOperationExceptionUtility.ThrowIf(
            condition: _startTask == null || _swarm == null,
            message: "Swarm has been stopped.");

        var blockChain = BlockChain;
        var height = blockChain.Tip.Index + 1;
        var manualResetEvent = _eventByTxId.GetOrAdd(transaction.Id, _ =>
        {
            return new ManualResetEvent(initialState: false);
        });
        blockChain.StageTransaction(transaction);
        await Task.Run(manualResetEvent.WaitOne, cancellationToken);

        _eventByTxId.TryRemove(transaction.Id, out _);

        var sb = new StringBuilder();
        foreach (var item in transaction.Actions)
        {
            if (_exceptionByAction.TryRemove(item, out var exception) == true &&
                exception is UnexpectedlyTerminatedActionException)
            {
                sb.AppendLine($"{exception.InnerException}");
            }
        }

        if (sb.Length > 0)
        {
            throw new InvalidOperationException(sb.ToString());
        }
    }

    public async Task<long> GetNextNonceAsync(Address address, CancellationToken cancellationToken)
    {
        ObjectDisposedExceptionUtility.ThrowIf(_isDisposed, this);
        InvalidOperationExceptionUtility.ThrowIf(
            condition: _startTask == null || _swarm == null,
            message: "Swarm has been stopped.");

        var blockChain = BlockChain;
        var nonce = blockChain.GetNextTxNonce(address);
        await Task.CompletedTask;
        return nonce;
    }

    public async ValueTask DisposeAsync()
    {
        ObjectDisposedExceptionUtility.ThrowIf(_isDisposed, this);

        DnsEndPointUtility.Release(ref _blocksyncEndPoint);
        DnsEndPointUtility.Release(ref _consensusEndPoint);

        if (_swarm != null)
        {
            await _swarm.StopAsync(cancellationToken: default);
            _swarm.Dispose();
        }

        await (_startTask ?? Task.CompletedTask);
        _startTask = null;
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    void IRenderer.RenderBlock(Block oldTip, Block newTip)
    {
        _synchronizationContext.Post(Action, state: null);

        void Action(object? state)
        {
            var height = newTip.Index;
            foreach (var transaction in newTip.Transactions)
            {
                if (_eventByTxId.TryGetValue(transaction.Id, out var manualResetEvent) == true)
                {
                    manualResetEvent.Set();
                }
            }

            var blockChain = _swarm!.BlockChain;
            var blockInfo = new BlockInfo(blockChain.Tip);

            BlockAppended?.Invoke(this, new(blockInfo));
        }
    }

    void IActionRenderer.RenderAction(
        IValue action, ICommittedActionContext context, HashDigest<SHA256> nextState)
    {
    }

    void IActionRenderer.RenderActionError(
        IValue action, ICommittedActionContext context, Exception exception)
    {
        _exceptionByAction.AddOrUpdate(action, exception, (_, _) => exception);
    }

    void IActionRenderer.RenderBlockEnd(Block oldTip, Block newTip)
    {
    }

    private static async Task<NetMQTransport> CreateTransport(
        PrivateKey privateKey, DnsEndPoint endPoint, CancellationToken cancellationToken)
    {
        var appProtocolVersionOptions = new AppProtocolVersionOptions
        {
            AppProtocolVersion = BlockChainUtility.AppProtocolVersion,
        };
        var hostOptions = new HostOptions(endPoint.Host, [], endPoint.Port);
        return await NetMQTransport.Create(privateKey, appProtocolVersionOptions, hostOptions);
    }
}
