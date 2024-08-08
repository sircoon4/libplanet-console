using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Store.Trie;

namespace LibplanetConsole.Explorer.GraphTypes
{
    public class WithdrawalInfo
    {
        public WithdrawalInfo(
            long nonce,
            Address from,
            Address to,
            long amount)
        {
            Nonce = nonce;
            From = from;
            To = to;
            Amount = amount;
        }

        public long Nonce { get; private set; }

        public Address From { get; private set; }

        public Address To { get; private set; }

        public long Amount { get; private set; }
    }
}
