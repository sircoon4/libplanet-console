using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Common;
using Libplanet.Store.Trie;

namespace LibplanetConsole.Explorer.GraphTypes
{
    public class WithdrawalProof
    {
        public WithdrawalProof(WithdrawalInfo withdrawalInfo, IValue proof)
        {
            WithdrawalInfo = withdrawalInfo;
            Proof = proof;
        }

        public WithdrawalInfo WithdrawalInfo { get; private set; }

        public IValue Proof { get; private set; }
    }
}
