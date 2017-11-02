using ACMESharp.Vault.Profile;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Certify.Core.Tests
{
    [TestClass]
    public class VaultManagerTest
    {
        [TestMethod]
        public void TestInitVault()
        {
            VaultProfileManager.SetProfile("SqlLiteVault", "SqlLiteVault");
            var vaultManager = new VaultManager(@"C:\ProgramData\Certify\Tests", "Vault.db", "SqlLiteVault");

            var vaultOK = vaultManager.InitVault();

            Assert.IsTrue(vaultOK, "Vault initialised");
        }
    }
}