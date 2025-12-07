using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfApp1;

namespace WpfApp1.Tests.Unit
{
    [TestClass]
    public class AiServiceTests
    {
        // Giả sử bạn có hàm: Task<string> AskAsync(string question)
        // và property IsDemo để không gọi API thật.

        private AiService CreateDemoService()
        {
            // isDemo = true => không gọi API thật
            return new AiService(isDemo: true);
        }


        [TestMethod]
        public async Task TC53_AiService_EmptyQuestion_Validation()
        {
            var svc = CreateDemoService();

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            {
                await svc.AskAsync(string.Empty);
            });
        }

        [TestMethod]
        public async Task TC54_AiService_LongQuestion_Validation()
        {
            var svc = CreateDemoService();

            var longQuestion = new string('a', 2501); // > 2000 ký tự

            var reply = await svc.AskAsync(longQuestion);

            StringAssert.Contains(reply, "quá dài");
        }
    }
}
