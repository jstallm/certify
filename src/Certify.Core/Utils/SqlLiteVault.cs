using ACMESharp.Vault;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Vault.Model;
using System.IO;
using ACMESharp.Ext;
using SQLite;

namespace Certify.Utils
{
    public class SqlLiteVaultAsset : VaultAsset
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public SqlLiteVaultAsset()
        {
        }

        public SqlLiteVaultAsset(string name, VaultAssetType type, bool isSensitive)
        {
            Name = name;
            Type = type;
            IsSensitive = isSensitive;
        }
    }

    public class SqlLiteVaultInfo
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public VaultInfo Info { get; set; }
    }

    public class SqlLiteDbInfo
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int SchemaVersion { get; set; }
        public string DbType { get; set; }
        public DateTime DateCreated { get; set; }
    }

    [VaultProvider(PROVIDER_NAME, Label = "SqlLiteDB Vault", Description = "Vault provider based on SqlLite")]
    public class SqlLiteVaultProvider : IVaultProvider
    {
        public const string PROVIDER_NAME = "local";

        public static readonly ParameterDetail ROOT_PATH = new ParameterDetail(
                nameof(SqlLiteVault.RootPath), ParameterType.TEXT,
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
            var vault = new SqlLiteVault();
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

    public class SqlLiteVault : IVault
    {
        public bool IsDisposed => true;

        public bool IsStorageOpen => true;

        public string RootPath
        { get; set; } = "C:\\ProgramData\\Certify\\";

        private const string VaultDBName = "vault.db";

        private SQLiteConnection GetVaultDataStore()
        {
            if (RootPath == null) RootPath = "C:\\ProgramData\\Certify\\";
            var dbPath = Path.Combine(new string[] { RootPath, VaultDBName });

            var db = new SQLiteConnection(dbPath);

            return db;
        }

        public void Dispose()
        {
        }

        public VaultAsset GetAsset(VaultAssetType type, string name)
        {
            using (var db = GetVaultDataStore())
            {
                return null;
                /*
                var col = db.GetCollection<LiteDBVaultAsset>("Assets");
                var asset = col.FindOne(q => q.Name == name && q.Type == type);
                if (asset == null) throw new KeyNotFoundException("Asset does not exist in vault database.");
                return asset;*/
            }
        }

        public VaultAsset CreateAsset(VaultAssetType type, string name, bool isSensitive = false, bool getOrCreate = false)
        {
            using (var db = GetVaultDataStore())
            {
                /*var a = new LiteDBVaultAsset(name, type, isSensitive);
                var col = db.GetCollection<LiteDBVaultAsset>("Assets");

                col.Insert(a);

                col.EnsureIndex(x => x.Id);
                return a;*/
                return null;
            }
        }

        public void InitStorage(bool force = false)
        {
            using (var db = GetVaultDataStore())
            {
                db.CreateTable<SqlLiteVaultAsset>();
                db.CreateTable<SqlLiteVaultInfo>();
                db.CreateTable<SqlLiteDbInfo>();

                var dbInfo = db.Table<SqlLiteDbInfo>().FirstOrDefault();

                if (dbInfo == null)
                {
                    //begin new vault database
                    db.Insert(new SqlLiteDbInfo { DateCreated = DateTime.UtcNow, DbType = "SqlLiteDB", SchemaVersion = 1 });
                }
            }
        }

        public IEnumerable<VaultAsset> ListAssets(string nameRegex = null, params VaultAssetType[] type)
        {
            using (var db = GetVaultDataStore())
            {
                return db.Table<SqlLiteVaultAsset>().AsEnumerable();
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
                var results = db.Table<SqlLiteVaultInfo>();
                try
                {
                    if (results != null && results.Any())
                    {
                        return results.First().Info;
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
                var existingVault = db.Table<SqlLiteVaultInfo>().FirstOrDefault();
                if (existingVault != null)
                {
                    existingVault.Info = vault;
                    db.Update(existingVault);
                }
                else
                {
                    db.Insert(new LiteDbVaultInfo { Info = vault });
                }
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