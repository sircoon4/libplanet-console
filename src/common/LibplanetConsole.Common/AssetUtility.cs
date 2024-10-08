using System.Collections.Immutable;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Tx;

namespace LibplanetConsole.Common;

public static class AssetUtility
{
    public static Address GetWithdrawalAccountAddress()
    {
        HashDigest<SHA1> hash = HashDigest<SHA1>.DeriveFrom(
            Encoding.ASCII.GetBytes("libplanet_withdraw"));
        Address address = new Address(hash.ToByteArray());
        return address;
    }

    public static Address GetWithdrawalDataAddress(TxId? txId)
    {
        HashDigest<SHA1> hash = HashDigest<SHA1>.DeriveFrom(
            txId?.ToByteArray() ??
            Encoding.ASCII.GetBytes("libplanet_withdraw_data"));
        Address address = new Address(hash.ToByteArray());
        return address;
    }

    public static FungibleAssetValue GetWETH(BigInteger amount)
    {
        var minters = ImmutableHashSet<Address>.Empty;
        minters = minters.Add(new Address("CE70F2e49927D431234BFc8D439412eef3a6276b"));
        Currency weth = Currency.Uncapped("WETH", 18, minters);
        return FungibleAssetValue.FromRawValue(weth, amount);
    }

    public static FungibleAssetValue GetNCG(BigInteger amount)
    {
        var minters = ImmutableHashSet<Address>.Empty;
        minters = minters.Add(new Address("CE70F2e49927D431234BFc8D439412eef3a6276b"));
        Currency ncg = Currency.Uncapped("NCG", 2, minters);
        return FungibleAssetValue.FromRawValue(ncg, amount);
    }
}
