using System.Collections.Immutable;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace LibplanetConsole.Common;

public static class AssetUtility
{
    public static FungibleAssetValue GetWETH(BigInteger amount)
    {
        var minters = ImmutableHashSet<Address>.Empty;
        minters = minters.Add(new Address("CE70F2e49927D431234BFc8D439412eef3a6276b"));
        Currency weth = Currency.Uncapped("WETH", 18, minters);
        return FungibleAssetValue.FromRawValue(weth, amount);
    }
}
