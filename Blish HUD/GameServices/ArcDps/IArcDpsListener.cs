namespace Blish_HUD.ArcDps
{
    public interface IArcDpsListener
    {
        event SocketListener.Message ReceivedMessage;
        void Start();
        void Stop();
    }
}