using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Transactions;

namespace WpfApp1.Tests.Common
{
    public abstract class DbTestBase
    {
        private TransactionScope _scope;

        [TestInitialize]
        public void Begin()
        {
            // Cho phép async flow nếu cần
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        [TestCleanup]
        public void Rollback() => _scope?.Dispose(); // auto-rollback
    }
}
