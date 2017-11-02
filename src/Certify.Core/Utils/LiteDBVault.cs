using ACMESharp.Vault;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Vault.Model;
using System.IO;
using LiteDB;
using ACMESharp.Ext;

namespace Certify.Utils
{
    public class LiteDBVaultAsset : VaultAsset
    {
        [BsonId]
        public Guid Id { get; set; }

        public LiteDBVaultAsset(string name, VaultAssetType type, bool isSensitive)
        {
            Name = name;
            Type = type;
            IsSensitive = isSensitive;
        }
    }

    public class LiteDbVaultInfo
    {
        [BsonId]
        public Guid Id { get; set; }

        public VaultInfo Info { get; set; }
    }

    public class DbInfo
    {
        [BsonId]
        public Guid Id { get; set; }

        public int SchemaVersion { get; set; }
        public string DbType { get; set; }
        public DateTime DateCreated { get; set; }
    }

    [VaultProvider(PROVIDER_NAME, Label = "LiteDB Vault", Description = "Vault provider based on LiteDB")]
    public class LiteDBVaultProvider : IVaultProvider
    {
        public const string PROVIDER_NAME = "local";

        public static readonly ParameterDetail ROOT_PATH = new ParameterDetail(
                nameof(LiteDBVault.RootPath), ParameterType.TEXT,
                isRequired: true, label: "Root Path",
                desc: "Specifies the directory path where vault data files will be stored.");

        private static readonly ParameterDetail[] PARAMS =
        {
            ROOT_PATH
        };

        public IEnumerable<ParameterDetail> DescribeParameters()
        {
            return PARAMS;
        }

        public IVault GetVault(IReadOnlyDictionary<string, object> initParams)
        {
            var vault = new LiteDBVault();
            if (initParams != null)
            {
                if (initParams.ContainsKey(ROOT_PATH.Name))
                    vault.RootPath = initParams[ROOT_PATH.Name] as string;
            }

            //vault.Init();

            return vault;
        }

        public void Dispose()
        { }
    }

    public class LiteDBVault : IVault
    {
        public bool IsDisposed => true;

        public bool IsStorageOpen => true;

        public string RootPath
        { get; set; } = "C:\\ProgramData\\Certify\\";

        private const string VaultDBName = "vault.db";

        private LiteDatabase GetVaultDataStore()
        {
            if (RootPath == null) RootPath = "C:\\ProgramData\\Certify\\";
            var dbPath = Path.Combine(new string[] { RootPath, VaultDBName });
            return new LiteDatabase(dbPath);
        }

        public VaultAsset CreateAsset(VaultAssetType type, string name, bool isSensitive = false, bool getOrCreate = false)
        {
            using (var db = GetVaultDataStore())
            {
                var a = new LiteDBVaultAsset(name, type, isSensitive);
                var col = db.GetCollection<LiteDBVaultAsset>("Assets");

                col.Insert(a);

                col.EnsureIndex(x => x.Id);
                return a;
            }
        }

        public void Dispose()
        {
        }

        public VaultAsset GetAsset(VaultAssetType type, string name)
        {
            using (var db = GetVaultDataStore())
            {
                var col = db.GetCollection<LiteDBVaultAsset>("Assets");
                var asset = col.FindOne(q => q.Name == name && q.Type == type);
                if (asset == null) throw new KeyNotFoundException("Asset does not exist in vault database.");
                return asset;
            }
        }

        public void InitStorage(bool force = false)
        {
            using (var db = GetVaultDataStore())
            {
                var col = db.GetCollection<DbInfo>("DbInfo");
                var dbInfo = col.FindOne(q => q.SchemaVersion > 0);
                if (dbInfo == null)
                {
                    //begin new vault database
                    col.Insert(new DbInfo { DateCreated = DateTime.UtcNow, DbType = "LiteDB", SchemaVersion = 1 });

                    col.EnsureIndex(x => x.Id);
                }
            }
        }

        public IEnumerable<VaultAsset> ListAssets(string nameRegex = null, params VaultAssetType[] type)
        {
            using (var db = GetVaultDataStore())
            {
                var col = db.GetCollection<LiteDBVaultAsset>("Assets");
                return col.FindAll().AsEnumerable();
            }
        }

        /// <summary>
        /// get stream to load asset file from 
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public Stream LoadAsset(VaultAsset asset)
        {
            throw new NotImplementedException();
        }

        public VaultInfo LoadVault(bool required = true)
        {
            System.Diagnostics.Debug.WriteLine("Dev Warning: full vault loaded. Vault items should be queried without process full vault");

            using (var db = GetVaultDataStore())
            {
                var col = db.GetCollection<LiteDbVaultInfo>("VaultInfo");
                try
                {
                    var vaultCount = col.Count();

                    var vaultInfo = col.FindAll();

                    if (vaultInfo != null)
                    {
                        return vaultInfo.First().Info;
                    }
                }
                catch (NullReferenceException exp)
                {
                }
                return null;
            }
        }

        public void OpenStorage(bool initOrOpen = false)
        {
            InitStorage();
        }

        /// <summary>
        /// Return file stream to save asset to 
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public Stream SaveAsset(VaultAsset asset)
        {
            throw new NotImplementedException();
        }

        public void SaveVault(VaultInfo vault)
        {
            using (var db = GetVaultDataStore())
            {
                var col = db.GetCollection<LiteDbVaultInfo>("VaultInfo");

                var existingVault = col.FindOne(q => q != null);
                if (existingVault != null)
                {
                    existingVault.Info = vault;
                    col.Update(existingVault);
                }
                else
                {
                    col.Insert(new LiteDbVaultInfo { Info = vault });
                }
                col.EnsureIndex(x => x.Id);
            }
        }

        public bool TestStorage()
        {
            try
            {
                using (var db = GetVaultDataStore())
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}