using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Transactions;

namespace WpfApp1.Tests.Common
{
    /// <summary>
    /// Base class cho các test dùng DB.
    /// Mỗi test chạy trong TransactionScope và rollback khi kết thúc.
    /// </summary>
    public abstract class DbTestBase
    {
        private TransactionScope _scope;

        [TestInitialize]
        public void Begin()
        {
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        [TestCleanup]
        public void Rollback() => _scope?.Dispose();
    }
}
