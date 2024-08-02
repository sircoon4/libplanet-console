using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace LibplanetConsole.Common.Actions;

[ActionType(ActionTypeValue)]
public sealed class WithdrawETHAction : ActionBase
{
    private const string ActionTypeValue = "withdraw_action";

    public WithdrawETHAction(Address recipient, BigInteger amount)
    {
        Recipient = recipient;
        Amount = amount;
    }

    public WithdrawETHAction()
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
        Address dataAddress = GetDataAddress();

        Codec codec = new Codec();

        var world = context.PreviousState;
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

        account = account
            .SetState(nonceAddress, nonce)
            .SetState(dataAddress, data)
            .SetState(hashAddress, new Bencodex.Types.Boolean(true));
        world = world.SetAccount(withdrawAddress, account);

        // To do: Transfer the WETH to the withdraw address.

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

    private Address GetDataAddress()
    {
        HashDigest<SHA1> hash = HashDigest<SHA1>.DeriveFrom(
            Encoding.ASCII.GetBytes("libplanet_withdraw_data"));
        Address address = new Address(hash.ToByteArray());
        return address;
    }
}
