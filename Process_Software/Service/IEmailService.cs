using Process_Software.Helpter;

namespace Process_Software.Service
{
    public interface IEmailService
    {
        void SendEmail(Mailrequest mailrequest);
    }
}