using System;
using System.Net.Sockets;
using System.Windows.Forms;

namespace SohbetistemciArayuz;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		using FormGiris formGiris = new FormGiris();
		if (formGiris.ShowDialog() == DialogResult.OK)
		{
			try
			{
				string kullaniciAdi = formGiris.KullaniciAdi;
				string ıpAdresi = formGiris.IpAdresi;
				TcpClient tcpClient = new TcpClient(ıpAdresi, 8888);
				NetworkStream stream = tcpClient.GetStream();
				Application.Run(new RealSound(kullaniciAdi, tcpClient, stream, ıpAdresi));
				return;
			}
			catch (SocketException ex)
			{
				MessageBox.Show("Bağlantı hatası: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				return;
			}
		}
	}
}
