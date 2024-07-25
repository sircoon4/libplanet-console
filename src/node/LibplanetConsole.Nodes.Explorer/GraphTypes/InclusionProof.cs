using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Common;
using Libplanet.Store.Trie;

namespace LibplanetConsole.Explorer.GraphTypes
{
    public class InclusionProof
    {
        public InclusionProof(
            HashDigest<SHA256> stateRootHash,
            IValue proof,
            KeyBytes key,
            IValue value)
        {
            StateRootHash = stateRootHash;
            Proof = proof;
            Key = key;
            Value = value;
        }

        public HashDigest<SHA256> StateRootHash { get; private set; }

        public IValue Proof { get; private set; }

        public KeyBytes Key { get; private set; }

        public IValue Value { get; private set; }
    }
}
