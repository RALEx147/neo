﻿using Neo.Core;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Caching;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using VMArray = Neo.VM.Types.Array;
using VMBoolean = Neo.VM.Types.Boolean;

namespace Neo.SmartContract
{
    public class StateReader : InteropService, IDisposable
    {
        public static event EventHandler<NotifyEventArgs> Notify;
        public static event EventHandler<LogEventArgs> Log;

        private readonly List<NotifyEventArgs> notifications = new List<NotifyEventArgs>();
        private readonly List<IDisposable> disposables = new List<IDisposable>();

        public IReadOnlyList<NotifyEventArgs> Notifications => notifications;

        private DataCache<UInt160, AccountState> _accounts;
        protected virtual DataCache<UInt160, AccountState> Accounts
        {
            get
            {
                if (_accounts == null)
                    _accounts = Blockchain.Default.GetStates<UInt160, AccountState>();
                return _accounts;
            }
        }

        private DataCache<UInt256, AssetState> _assets;
        protected virtual DataCache<UInt256, AssetState> Assets
        {
            get
            {
                if (_assets == null)
                    _assets = Blockchain.Default.GetStates<UInt256, AssetState>();
                return _assets;
            }
        }

        private DataCache<UInt160, ContractState> _contracts;
        protected virtual DataCache<UInt160, ContractState> Contracts
        {
            get
            {
                if (_contracts == null)
                    _contracts = Blockchain.Default.GetStates<UInt160, ContractState>();
                return _contracts;
            }
        }

        private DataCache<StorageKey, StorageItem> _storages;
        protected virtual DataCache<StorageKey, StorageItem> Storages
        {
            get
            {
                if (_storages == null)
                    _storages = Blockchain.Default.GetStates<StorageKey, StorageItem>();
                return _storages;
            }
        }

        public StateReader()
        {
            Register("Neo.Runtime.GetTrigger", Runtime_GetTrigger);
            Register("Neo.Runtime.CheckWitness", Runtime_CheckWitness);
            Register("Neo.Runtime.Notify", Runtime_Notify);
            Register("Neo.Runtime.Log", Runtime_Log);
            Register("Neo.Runtime.GetTime", Runtime_GetTime);
            Register("Neo.Runtime.Serialize", Runtime_Serialize);
            Register("Neo.Runtime.Deserialize", Runtime_Deserialize);
            Register("Neo.Blockchain.GetHeight", Blockchain_GetHeight);
            Register("Neo.Blockchain.GetHeader", Blockchain_GetHeader);
            Register("Neo.Blockchain.GetBlock", Blockchain_GetBlock);
            Register("Neo.Blockchain.GetTransaction", Blockchain_GetTransaction);
            Register("Neo.Blockchain.GetAccount", Blockchain_GetAccount);
            Register("Neo.Blockchain.GetValidators", Blockchain_GetValidators);
            Register("Neo.Blockchain.GetAsset", Blockchain_GetAsset);
            Register("Neo.Blockchain.GetContract", Blockchain_GetContract);
            Register("Neo.Header.GetIndex", Header_GetIndex);
            Register("Neo.Header.GetHash", Header_GetHash);
            Register("Neo.Header.GetVersion", Header_GetVersion);
            Register("Neo.Header.GetPrevHash", Header_GetPrevHash);
            Register("Neo.Header.GetMerkleRoot", Header_GetMerkleRoot);
            Register("Neo.Header.GetTimestamp", Header_GetTimestamp);
            Register("Neo.Header.GetConsensusData", Header_GetConsensusData);
            Register("Neo.Header.GetNextConsensus", Header_GetNextConsensus);
            Register("Neo.Block.GetTransactionCount", Block_GetTransactionCount);
            Register("Neo.Block.GetTransactions", Block_GetTransactions);
            Register("Neo.Block.GetTransaction", Block_GetTransaction);
            Register("Neo.Transaction.GetHash", Transaction_GetHash);
            Register("Neo.Transaction.GetType", Transaction_GetType);
            Register("Neo.Transaction.GetAttributes", Transaction_GetAttributes);
            Register("Neo.Transaction.GetInputs", Transaction_GetInputs);
            Register("Neo.Transaction.GetOutputs", Transaction_GetOutputs);
            Register("Neo.Transaction.GetReferences", Transaction_GetReferences);
            Register("Neo.Transaction.GetUnspentCoins", Transaction_GetUnspentCoins);
            Register("Neo.Attribute.GetUsage", Attribute_GetUsage);
            Register("Neo.Attribute.GetData", Attribute_GetData);
            Register("Neo.Input.GetHash", Input_GetHash);
            Register("Neo.Input.GetIndex", Input_GetIndex);
            Register("Neo.Output.GetAssetId", Output_GetAssetId);
            Register("Neo.Output.GetValue", Output_GetValue);
            Register("Neo.Output.GetScriptHash", Output_GetScriptHash);
            Register("Neo.Account.GetScriptHash", Account_GetScriptHash);
            Register("Neo.Account.GetVotes", Account_GetVotes);
            Register("Neo.Account.GetBalance", Account_GetBalance);
            Register("Neo.Asset.GetAssetId", Asset_GetAssetId);
            Register("Neo.Asset.GetAssetType", Asset_GetAssetType);
            Register("Neo.Asset.GetAmount", Asset_GetAmount);
            Register("Neo.Asset.GetAvailable", Asset_GetAvailable);
            Register("Neo.Asset.GetPrecision", Asset_GetPrecision);
            Register("Neo.Asset.GetOwner", Asset_GetOwner);
            Register("Neo.Asset.GetAdmin", Asset_GetAdmin);
            Register("Neo.Asset.GetIssuer", Asset_GetIssuer);
            Register("Neo.Contract.GetScript", Contract_GetScript);
            Register("Neo.Storage.GetContext", Storage_GetContext);
            Register("Neo.Storage.Get", Storage_Get);
            Register("Neo.Storage.Find", Storage_Find);
            Register("Neo.Iterator.Next", Iterator_Next);
            Register("Neo.Iterator.Key", Iterator_Key);
            Register("Neo.Iterator.Value", Iterator_Value);
            #region Old AntShares APIs
            Register("AntShares.Runtime.CheckWitness", Runtime_CheckWitness);
            Register("AntShares.Runtime.Notify", Runtime_Notify);
            Register("AntShares.Runtime.Log", Runtime_Log);
            Register("AntShares.Blockchain.GetHeight", Blockchain_GetHeight);
            Register("AntShares.Blockchain.GetHeader", Blockchain_GetHeader);
            Register("AntShares.Blockchain.GetBlock", Blockchain_GetBlock);
            Register("AntShares.Blockchain.GetTransaction", Blockchain_GetTransaction);
            Register("AntShares.Blockchain.GetAccount", Blockchain_GetAccount);
            Register("AntShares.Blockchain.GetValidators", Blockchain_GetValidators);
            Register("AntShares.Blockchain.GetAsset", Blockchain_GetAsset);
            Register("AntShares.Blockchain.GetContract", Blockchain_GetContract);
            Register("AntShares.Header.GetHash", Header_GetHash);
            Register("AntShares.Header.GetVersion", Header_GetVersion);
            Register("AntShares.Header.GetPrevHash", Header_GetPrevHash);
            Register("AntShares.Header.GetMerkleRoot", Header_GetMerkleRoot);
            Register("AntShares.Header.GetTimestamp", Header_GetTimestamp);
            Register("AntShares.Header.GetConsensusData", Header_GetConsensusData);
            Register("AntShares.Header.GetNextConsensus", Header_GetNextConsensus);
            Register("AntShares.Block.GetTransactionCount", Block_GetTransactionCount);
            Register("AntShares.Block.GetTransactions", Block_GetTransactions);
            Register("AntShares.Block.GetTransaction", Block_GetTransaction);
            Register("AntShares.Transaction.GetHash", Transaction_GetHash);
            Register("AntShares.Transaction.GetType", Transaction_GetType);
            Register("AntShares.Transaction.GetAttributes", Transaction_GetAttributes);
            Register("AntShares.Transaction.GetInputs", Transaction_GetInputs);
            Register("AntShares.Transaction.GetOutputs", Transaction_GetOutputs);
            Register("AntShares.Transaction.GetReferences", Transaction_GetReferences);
            Register("AntShares.Attribute.GetUsage", Attribute_GetUsage);
            Register("AntShares.Attribute.GetData", Attribute_GetData);
            Register("AntShares.Input.GetHash", Input_GetHash);
            Register("AntShares.Input.GetIndex", Input_GetIndex);
            Register("AntShares.Output.GetAssetId", Output_GetAssetId);
            Register("AntShares.Output.GetValue", Output_GetValue);
            Register("AntShares.Output.GetScriptHash", Output_GetScriptHash);
            Register("AntShares.Account.GetScriptHash", Account_GetScriptHash);
            Register("AntShares.Account.GetVotes", Account_GetVotes);
            Register("AntShares.Account.GetBalance", Account_GetBalance);
            Register("AntShares.Asset.GetAssetId", Asset_GetAssetId);
            Register("AntShares.Asset.GetAssetType", Asset_GetAssetType);
            Register("AntShares.Asset.GetAmount", Asset_GetAmount);
            Register("AntShares.Asset.GetAvailable", Asset_GetAvailable);
            Register("AntShares.Asset.GetPrecision", Asset_GetPrecision);
            Register("AntShares.Asset.GetOwner", Asset_GetOwner);
            Register("AntShares.Asset.GetAdmin", Asset_GetAdmin);
            Register("AntShares.Asset.GetIssuer", Asset_GetIssuer);
            Register("AntShares.Contract.GetScript", Contract_GetScript);
            Register("AntShares.Storage.GetContext", Storage_GetContext);
            Register("AntShares.Storage.Get", Storage_Get);
            #endregion
        }

        internal bool CheckStorageContext(StorageContext context)
        {
            ContractState contract = Contracts.TryGet(context.ScriptHash);
            if (contract == null) return false;
            if (!contract.HasStorage) return false;
            return true;
        }

        public void Dispose()
        {
            foreach (IDisposable disposable in disposables)
                disposable.Dispose();
            disposables.Clear();
        }

        protected virtual bool Runtime_GetTrigger(ExecutionEngine engine)
        {
            ApplicationEngine app_engine = (ApplicationEngine)engine;
            engine.EvaluationStack.Push((int)app_engine.Trigger);
            return true;
        }

        protected bool CheckWitness(ExecutionEngine engine, UInt160 hash)
        {
            IVerifiable container = (IVerifiable)engine.ScriptContainer;
            UInt160[] _hashes_for_verifying = container.GetScriptHashesForVerifying();
            return _hashes_for_verifying.Contains(hash);
        }

        protected bool CheckWitness(ExecutionEngine engine, ECPoint pubkey)
        {
            return CheckWitness(engine, Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash());
        }

        protected virtual bool Runtime_CheckWitness(ExecutionEngine engine)
        {
            byte[] hashOrPubkey = engine.EvaluationStack.Pop().GetByteArray();
            bool result;
            if (hashOrPubkey.Length == 20)
                result = CheckWitness(engine, new UInt160(hashOrPubkey));
            else if (hashOrPubkey.Length == 33)
                result = CheckWitness(engine, ECPoint.DecodePoint(hashOrPubkey, ECCurve.Secp256r1));
            else
                return false;
            engine.EvaluationStack.Push(result);
            return true;
        }

        protected virtual bool Runtime_Notify(ExecutionEngine engine)
        {
            StackItem state = engine.EvaluationStack.Pop();
            NotifyEventArgs notification = new NotifyEventArgs(engine.ScriptContainer, new UInt160(engine.CurrentContext.ScriptHash), state);
            Notify?.Invoke(this, notification);
            notifications.Add(notification);
            return true;
        }

        protected virtual bool Runtime_Log(ExecutionEngine engine)
        {
            string message = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            Log?.Invoke(this, new LogEventArgs(engine.ScriptContainer, new UInt160(engine.CurrentContext.ScriptHash), message));
            return true;
        }

        protected virtual bool Runtime_GetTime(ExecutionEngine engine)
        {
            BlockBase header = Blockchain.Default?.GetHeader(Blockchain.Default.Height);
            if (header == null) header = Blockchain.GenesisBlock;
            engine.EvaluationStack.Push(header.Timestamp + Blockchain.SecondsPerBlock);
            return true;
        }

        private void SerializeStackItem(StackItem item, BinaryWriter writer)
        {
            switch (item)
            {
                case ByteArray _:
                    writer.Write((byte)StackItemType.ByteArray);
                    writer.WriteVarBytes(item.GetByteArray());
                    break;
                case VMBoolean _:
                    writer.Write((byte)StackItemType.Boolean);
                    writer.Write(item.GetBoolean());
                    break;
                case Integer _:
                    writer.Write((byte)StackItemType.Integer);
                    writer.WriteVarBytes(item.GetByteArray());
                    break;
                case InteropInterface _:
                    throw new NotSupportedException();
                case VMArray array:
                    if (array is Struct)
                        writer.Write((byte)StackItemType.Struct);
                    else
                        writer.Write((byte)StackItemType.Array);
                    writer.WriteVarInt(array.Count);
                    foreach (StackItem subitem in array)
                        SerializeStackItem(subitem, writer);
                    break;
            }
        }

        protected virtual bool Runtime_Serialize(ExecutionEngine engine)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                try
                {
                    SerializeStackItem(engine.EvaluationStack.Pop(), writer);
                }
                catch (NotSupportedException)
                {
                    return false;
                }
                writer.Flush();
                engine.EvaluationStack.Push(ms.ToArray());
            }
            return true;
        }

        private StackItem DeserializeStackItem(BinaryReader reader)
        {
            StackItemType type = (StackItemType)reader.ReadByte();
            switch (type)
            {
                case StackItemType.ByteArray:
                    return new ByteArray(reader.ReadVarBytes());
                case StackItemType.Boolean:
                    return new VMBoolean(reader.ReadBoolean());
                case StackItemType.Integer:
                    return new Integer(new BigInteger(reader.ReadVarBytes()));
                case StackItemType.Array:
                case StackItemType.Struct:
                    VMArray array = type == StackItemType.Struct ? new Struct() : new VMArray();
                    ulong count = reader.ReadVarInt();
                    while (count-- > 0)
                        array.Add(DeserializeStackItem(reader));
                    return array;
                default:
                    return null;
            }
        }

        protected virtual bool Runtime_Deserialize(ExecutionEngine engine)
        {
            byte[] data = engine.EvaluationStack.Pop().GetByteArray();
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                StackItem item = DeserializeStackItem(reader);
                if (item == null) return false;
                engine.EvaluationStack.Push(item);
            }
            return true;
        }

        protected virtual bool Blockchain_GetHeight(ExecutionEngine engine)
        {
            if (Blockchain.Default == null)
                engine.EvaluationStack.Push(0);
            else
                engine.EvaluationStack.Push(Blockchain.Default.Height);
            return true;
        }

        protected virtual bool Blockchain_GetHeader(ExecutionEngine engine)
        {
            byte[] data = engine.EvaluationStack.Pop().GetByteArray();
            Header header;
            if (data.Length <= 5)
            {
                uint height = (uint)new BigInteger(data);
                if (Blockchain.Default != null)
                    header = Blockchain.Default.GetHeader(height);
                else if (height == 0)
                    header = Blockchain.GenesisBlock.Header;
                else
                    header = null;
            }
            else if (data.Length == 32)
            {
                UInt256 hash = new UInt256(data);
                if (Blockchain.Default != null)
                    header = Blockchain.Default.GetHeader(hash);
                else if (hash == Blockchain.GenesisBlock.Hash)
                    header = Blockchain.GenesisBlock.Header;
                else
                    header = null;
            }
            else
            {
                return false;
            }
            engine.EvaluationStack.Push(StackItem.FromInterface(header));
            return true;
        }

        protected virtual bool Blockchain_GetBlock(ExecutionEngine engine)
        {
            byte[] data = engine.EvaluationStack.Pop().GetByteArray();
            Block block;
            if (data.Length <= 5)
            {
                uint height = (uint)new BigInteger(data);
                if (Blockchain.Default != null)
                    block = Blockchain.Default.GetBlock(height);
                else if (height == 0)
                    block = Blockchain.GenesisBlock;
                else
                    block = null;
            }
            else if (data.Length == 32)
            {
                UInt256 hash = new UInt256(data);
                if (Blockchain.Default != null)
                    block = Blockchain.Default.GetBlock(hash);
                else if (hash == Blockchain.GenesisBlock.Hash)
                    block = Blockchain.GenesisBlock;
                else
                    block = null;
            }
            else
            {
                return false;
            }
            engine.EvaluationStack.Push(StackItem.FromInterface(block));
            return true;
        }

        protected virtual bool Blockchain_GetTransaction(ExecutionEngine engine)
        {
            byte[] hash = engine.EvaluationStack.Pop().GetByteArray();
            Transaction tx = Blockchain.Default?.GetTransaction(new UInt256(hash));
            engine.EvaluationStack.Push(StackItem.FromInterface(tx));
            return true;
        }

        protected virtual bool Blockchain_GetAccount(ExecutionEngine engine)
        {
            UInt160 hash = new UInt160(engine.EvaluationStack.Pop().GetByteArray());
            AccountState account = Accounts.GetOrAdd(hash, () => new AccountState(hash));
            engine.EvaluationStack.Push(StackItem.FromInterface(account));
            return true;
        }

        protected virtual bool Blockchain_GetValidators(ExecutionEngine engine)
        {
            ECPoint[] validators = Blockchain.Default.GetValidators();
            engine.EvaluationStack.Push(validators.Select(p => (StackItem)p.EncodePoint(true)).ToArray());
            return true;
        }

        protected virtual bool Blockchain_GetAsset(ExecutionEngine engine)
        {
            UInt256 hash = new UInt256(engine.EvaluationStack.Pop().GetByteArray());
            AssetState asset = Assets.TryGet(hash);
            if (asset == null) return false;
            engine.EvaluationStack.Push(StackItem.FromInterface(asset));
            return true;
        }

        protected virtual bool Blockchain_GetContract(ExecutionEngine engine)
        {
            UInt160 hash = new UInt160(engine.EvaluationStack.Pop().GetByteArray());
            ContractState contract = Contracts.TryGet(hash);
            if (contract == null) return false;
            engine.EvaluationStack.Push(StackItem.FromInterface(contract));
            return true;
        }

        protected virtual bool Header_GetIndex(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.EvaluationStack.Push(header.Index);
                return true;
            }
            return false;
        }

        protected virtual bool Header_GetHash(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.EvaluationStack.Push(header.Hash.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Header_GetVersion(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.EvaluationStack.Push(header.Version);
                return true;
            }
            return false;
        }

        protected virtual bool Header_GetPrevHash(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.EvaluationStack.Push(header.PrevHash.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Header_GetMerkleRoot(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.EvaluationStack.Push(header.MerkleRoot.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Header_GetTimestamp(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.EvaluationStack.Push(header.Timestamp);
                return true;
            }
            return false;
        }

        protected virtual bool Header_GetConsensusData(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.EvaluationStack.Push(header.ConsensusData);
                return true;
            }
            return false;
        }

        protected virtual bool Header_GetNextConsensus(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.EvaluationStack.Push(header.NextConsensus.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Block_GetTransactionCount(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Block block = _interface.GetInterface<Block>();
                if (block == null) return false;
                engine.EvaluationStack.Push(block.Transactions.Length);
                return true;
            }
            return false;
        }

        protected virtual bool Block_GetTransactions(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Block block = _interface.GetInterface<Block>();
                if (block == null) return false;
                engine.EvaluationStack.Push(block.Transactions.Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Block_GetTransaction(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Block block = _interface.GetInterface<Block>();
                int index = (int)engine.EvaluationStack.Pop().GetBigInteger();
                if (block == null) return false;
                if (index < 0 || index >= block.Transactions.Length) return false;
                Transaction tx = block.Transactions[index];
                engine.EvaluationStack.Push(StackItem.FromInterface(tx));
                return true;
            }
            return false;
        }

        protected virtual bool Transaction_GetHash(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.EvaluationStack.Push(tx.Hash.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Transaction_GetType(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.EvaluationStack.Push((int)tx.Type);
                return true;
            }
            return false;
        }

        protected virtual bool Transaction_GetAttributes(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.EvaluationStack.Push(tx.Attributes.Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Transaction_GetInputs(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.EvaluationStack.Push(tx.Inputs.Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Transaction_GetOutputs(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.EvaluationStack.Push(tx.Outputs.Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Transaction_GetReferences(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.EvaluationStack.Push(tx.Inputs.Select(p => StackItem.FromInterface(tx.References[p])).ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Transaction_GetUnspentCoins(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.EvaluationStack.Push(Blockchain.Default.GetUnspent(tx.Hash).Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Attribute_GetUsage(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionAttribute attr = _interface.GetInterface<TransactionAttribute>();
                if (attr == null) return false;
                engine.EvaluationStack.Push((int)attr.Usage);
                return true;
            }
            return false;
        }

        protected virtual bool Attribute_GetData(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionAttribute attr = _interface.GetInterface<TransactionAttribute>();
                if (attr == null) return false;
                engine.EvaluationStack.Push(attr.Data);
                return true;
            }
            return false;
        }

        protected virtual bool Input_GetHash(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                CoinReference input = _interface.GetInterface<CoinReference>();
                if (input == null) return false;
                engine.EvaluationStack.Push(input.PrevHash.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Input_GetIndex(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                CoinReference input = _interface.GetInterface<CoinReference>();
                if (input == null) return false;
                engine.EvaluationStack.Push((int)input.PrevIndex);
                return true;
            }
            return false;
        }

        protected virtual bool Output_GetAssetId(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionOutput output = _interface.GetInterface<TransactionOutput>();
                if (output == null) return false;
                engine.EvaluationStack.Push(output.AssetId.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Output_GetValue(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionOutput output = _interface.GetInterface<TransactionOutput>();
                if (output == null) return false;
                engine.EvaluationStack.Push(output.Value.GetData());
                return true;
            }
            return false;
        }

        protected virtual bool Output_GetScriptHash(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionOutput output = _interface.GetInterface<TransactionOutput>();
                if (output == null) return false;
                engine.EvaluationStack.Push(output.ScriptHash.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Account_GetScriptHash(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AccountState account = _interface.GetInterface<AccountState>();
                if (account == null) return false;
                engine.EvaluationStack.Push(account.ScriptHash.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Account_GetVotes(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AccountState account = _interface.GetInterface<AccountState>();
                if (account == null) return false;
                engine.EvaluationStack.Push(account.Votes.Select(p => (StackItem)p.EncodePoint(true)).ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Account_GetBalance(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AccountState account = _interface.GetInterface<AccountState>();
                UInt256 asset_id = new UInt256(engine.EvaluationStack.Pop().GetByteArray());
                if (account == null) return false;
                Fixed8 balance = account.Balances.TryGetValue(asset_id, out Fixed8 value) ? value : Fixed8.Zero;
                engine.EvaluationStack.Push(balance.GetData());
                return true;
            }
            return false;
        }

        protected virtual bool Asset_GetAssetId(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.EvaluationStack.Push(asset.AssetId.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Asset_GetAssetType(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.EvaluationStack.Push((int)asset.AssetType);
                return true;
            }
            return false;
        }

        protected virtual bool Asset_GetAmount(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.EvaluationStack.Push(asset.Amount.GetData());
                return true;
            }
            return false;
        }

        protected virtual bool Asset_GetAvailable(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.EvaluationStack.Push(asset.Available.GetData());
                return true;
            }
            return false;
        }

        protected virtual bool Asset_GetPrecision(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.EvaluationStack.Push((int)asset.Precision);
                return true;
            }
            return false;
        }

        protected virtual bool Asset_GetOwner(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.EvaluationStack.Push(asset.Owner.EncodePoint(true));
                return true;
            }
            return false;
        }

        protected virtual bool Asset_GetAdmin(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.EvaluationStack.Push(asset.Admin.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Asset_GetIssuer(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.EvaluationStack.Push(asset.Issuer.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Contract_GetScript(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                ContractState contract = _interface.GetInterface<ContractState>();
                if (contract == null) return false;
                engine.EvaluationStack.Push(contract.Script);
                return true;
            }
            return false;
        }

        protected virtual bool Storage_GetContext(ExecutionEngine engine)
        {
            engine.EvaluationStack.Push(StackItem.FromInterface(new StorageContext
            {
                ScriptHash = new UInt160(engine.CurrentContext.ScriptHash)
            }));
            return true;
        }

        protected virtual bool Storage_Get(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                StorageContext context = _interface.GetInterface<StorageContext>();
                if (!CheckStorageContext(context)) return false;
                byte[] key = engine.EvaluationStack.Pop().GetByteArray();
                StorageItem item = Storages.TryGet(new StorageKey
                {
                    ScriptHash = context.ScriptHash,
                    Key = key
                });
                engine.EvaluationStack.Push(item?.Value ?? new byte[0]);
                return true;
            }
            return false;
        }

        protected virtual bool Storage_Find(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                StorageContext context = _interface.GetInterface<StorageContext>();
                if (!CheckStorageContext(context)) return false;
                byte[] prefix = engine.EvaluationStack.Pop().GetByteArray();
                prefix = context.ScriptHash.ToArray().Concat(prefix).ToArray();
                StorageIterator iterator = new StorageIterator(Storages.Find(prefix).GetEnumerator());
                engine.EvaluationStack.Push(StackItem.FromInterface(iterator));
                disposables.Add(iterator);
                return true;
            }
            return false;
        }

        protected virtual bool Iterator_Next(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Iterator iterator = _interface.GetInterface<Iterator>();
                engine.EvaluationStack.Push(iterator.Next());
                return true;
            }
            return false;
        }

        protected virtual bool Iterator_Key(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Iterator iterator = _interface.GetInterface<Iterator>();
                engine.EvaluationStack.Push(iterator.Key());
                return true;
            }
            return false;
        }

        protected virtual bool Iterator_Value(ExecutionEngine engine)
        {
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Iterator iterator = _interface.GetInterface<Iterator>();
                engine.EvaluationStack.Push(iterator.Value());
                return true;
            }
            return false;
        }
    }
}
