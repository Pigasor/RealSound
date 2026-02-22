using System.Net;

namespace SohbetSunucu;

public class UdpPacket
{
	public byte[] Data { get; set; }

	public IPEndPoint Sender { get; set; }
}
