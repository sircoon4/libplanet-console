using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Crypto;
using LibplanetConsole.Common;

namespace LibplanetConsole.Nodes;

public interface IBlockChain
{
    event EventHandler<BlockEventArgs>? BlockAppended;

    Task<AppId> AddTransactionAsync(IAction[] actions, CancellationToken cancellationToken);

    Task<AppId> AddTransactionWithPrivateKeyAsync(
        IAction[] actions, PrivateKey privateKey, CancellationToken cancellationToken);

    Task<long> GetNextNonceAsync(AppAddress address, CancellationToken cancellationToken);

    Task<AppHash> GetTipHashAsync(CancellationToken cancellationToken);

    Task<IValue> GetStateAsync(
        AppHash blockHash,
        AppAddress accountAddress,
        AppAddress address,
        CancellationToken cancellationToken);

    Task<IValue> GetStateByStateRootHashAsync(
        AppHash stateRootHash,
        AppAddress accountAddress,
        AppAddress address,
        CancellationToken cancellationToken);

    Task<AppHash> GetBlockHashAsync(long height, CancellationToken cancellationToken);

    Task<T> GetActionAsync<T>(AppId txId, int actionIndex, CancellationToken cancellationToken)
        where T : IAction;
}
