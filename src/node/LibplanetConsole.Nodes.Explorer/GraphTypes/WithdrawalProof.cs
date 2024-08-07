using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Common;
using Libplanet.Store.Trie;

namespace LibplanetConsole.Explorer.GraphTypes
{
    public class WithdrawalProof
    {
        public WithdrawalProof(
            IValue withdrawalInfo,
            IValue proof)
        {
            WithdrawalInfo = withdrawalInfo;
            Proof = proof;
        }

        public IValue WithdrawalInfo { get; private set; }

        public IValue Proof { get; private set; }
    }
}
