using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Serilog;

namespace LibplanetConsole.Common.Actions;

[ActionType(ActionTypeValue)]
public sealed class WithdrawEthAction : ActionBase
{
    private const string ActionTypeValue = "withdraw_action";

    public WithdrawEthAction(Address recipient, BigInteger amount)
    {
        Recipient = recipient;
        Amount = amount;
    }

    public WithdrawEthAction()
    {
    }

    public Address Recipient { get; set; }

    public BigInteger Amount { get; set; }

    protected override Dictionary OnInitialize(Dictionary values)
    {
        return base.OnInitialize(values)
            .Add("recipient", Recipient.Bencoded)
            .Add("amount", new Integer(Amount));
    }

    protected override void OnLoadPlainValue(Dictionary values)
    {
        base.OnLoadPlainValue(values);
        Recipient = new Address(values["recipient"]);
        Amount = ((Integer)values["amount"]).Value;
    }

    protected override IWorld OnExecute(IActionContext context)
    {
        Address withdrawAddress = GetWithdrawAddress();
        Address nonceAddress = GetNonceAddress();
        Address dataAddress = GetDataAddress(context.TxId);

        var world = context.PreviousState;

        try
        {
            var weth = AssetUtility.GetWETH(Amount);
            world = world.TransferAsset(context, context.Signer, withdrawAddress, weth);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to transfer WETH: {0}", e.Message);
            throw new InvalidOperationException("Failed to transfer WETH.", e);
        }

        Codec codec = new Codec();

        var account = world.GetAccount(withdrawAddress);

        var nonce = (Integer)(account.GetState(nonceAddress) ?? new Integer(0));
        nonce = nonce + 1;

        var data = Dictionary.Empty
            .Add("nonce", nonce)
            .Add("from", context.Signer.Bencoded)
            .Add("to", Recipient.Bencoded)
            .Add("amount", new Integer(Amount));

        var hash = HashDigest<SHA1>.DeriveFrom(codec.Encode(data));
        var hashAddress = new Address(hash.ToByteArray());

        data = data.Add("hash", hashAddress.Bencoded);

        account = account
            .SetState(nonceAddress, nonce)
            .SetState(dataAddress, data)
            .SetState(hashAddress, new Bencodex.Types.Boolean(true));
        world = world.SetAccount(withdrawAddress, account);

        return world;
    }

    private Address GetWithdrawAddress()
    {
        HashDigest<SHA1> hash = HashDigest<SHA1>.DeriveFrom(
            Encoding.ASCII.GetBytes("libplanet_withdraw"));
        Address address = new Address(hash.ToByteArray());
        return address;
    }

    private Address GetNonceAddress()
    {
        HashDigest<SHA1> hash = HashDigest<SHA1>.DeriveFrom(
            Encoding.ASCII.GetBytes("libplanet_withdraw_nonce"));
        Address address = new Address(hash.ToByteArray());
        return address;
    }

    private Address GetDataAddress(TxId? txId)
    {
        HashDigest<SHA1> hash = HashDigest<SHA1>.DeriveFrom(
            txId?.ToByteArray() ??
            Encoding.ASCII.GetBytes("libplanet_withdraw_data"));
        Address address = new Address(hash.ToByteArray());
        return address;
    }
}
