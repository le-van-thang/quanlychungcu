using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace WpfApp1
{
    public static class Mailer
    {
        private const string FROM = "levanthang0166@gmail.com";
        private const string APP_PASSWORD = "vytx njrx gnmw urzk";  // mật khẩu ứng dụng Google


        public static async Task SendAsync(string to, string subject, string body)
        {
            using (var c = new SmtpClient("smtp.gmail.com", 587))
            {
                c.EnableSsl = true;
                c.UseDefaultCredentials = false;                         // quan trọng
                c.Credentials = new NetworkCredential(FROM, APP_PASSWORD);

                using (var msg = new MailMessage())
                {
                    msg.From = new MailAddress(FROM);
                    msg.To.Add(to);
                    msg.Subject = subject;
                    msg.Body = body;
                    msg.IsBodyHtml = false;

                    await c.SendMailAsync(msg);
                }
            }
        }
    }
}
