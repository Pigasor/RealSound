using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SohbetSunucu;

internal class Program
{
	private static Dictionary<string, IPEndPoint> clientUdpEndpoints = new Dictionary<string, IPEndPoint>();

	private static Dictionary<string, TcpClient> clientTcpClients = new Dictionary<string, TcpClient>();

	private static TcpListener tcpListener;

	private static UdpClient udpServer;

	private static bool isServerRunning = false;

	private static BlockingCollection<UdpPacket> udpPacketQueue = new BlockingCollection<UdpPacket>();

	private static CancellationTokenSource cts = new CancellationTokenSource();

	private static void Main(string[] args)
	{
		Console.WriteLine("Sohbet Sunucusu Başlatılıyor...");
		isServerRunning = true;
		Task.Run(delegate
		{
			StartTcpServer(8888);
		});
		Task.Run(() => StartUdpReceiver(8888));
		Task.Run(() => ProcessUdpQueue(cts.Token));
		Console.WriteLine("Sunucu başlatıldı. Bağlantılar bekleniyor...");
		while (isServerRunning)
		{
			Thread.Sleep(100);
		}
	}

	private static void StartTcpServer(int port)
	{
		try
		{
			tcpListener = new TcpListener(IPAddress.Any, port);
			tcpListener.Start();
			while (true)
			{
				TcpClient client = tcpListener.AcceptTcpClient();
				Console.WriteLine($"Yeni bir TCP bağlantısı geldi: {((IPEndPoint)client.Client.RemoteEndPoint).Address}");
				Task.Run(() => HandleClient(client));
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("TCP Sunucu Hatası: " + ex.Message);
		}
	}

	private static async Task HandleClient(TcpClient tcpClient)
	{
		NetworkStream stream = tcpClient.GetStream();
		string clientUsername = "";
		try
		{
			byte[] portBuffer = new byte[1024];
			int portBytes = await stream.ReadAsync(portBuffer, 0, portBuffer.Length);
			string udpPortString = Encoding.UTF8.GetString(portBuffer, 0, portBytes);
			int udpPort = int.Parse(udpPortString);
			byte[] userBuffer = new byte[1024];
			int userBytes = await stream.ReadAsync(userBuffer, 0, userBuffer.Length);
			clientUsername = Encoding.UTF8.GetString(userBuffer, 0, userBytes);
			Console.WriteLine("İstemci '" + clientUsername + "' bağlandı.");
			lock (clientTcpClients)
			{
				clientUdpEndpoints[clientUsername] = new IPEndPoint(IPAddress.Parse(((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString()), udpPort);
				clientTcpClients[clientUsername] = tcpClient;
			}
			BroadcastUserList();
			BroadcastMessage(clientUsername + " sohbet odasına katıldı.");
			byte[] messageBuffer = new byte[1024];
			while (tcpClient.Connected)
			{
				int bytesRead = await stream.ReadAsync(messageBuffer, 0, messageBuffer.Length);
				if (bytesRead == 0)
				{
					break;
				}
				string message = Encoding.UTF8.GetString(messageBuffer, 0, bytesRead);
				Console.WriteLine("TCP'den Mesaj Alındı: " + message);
				BroadcastMessage(message);
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Console.WriteLine("İstemci bağlantısı koptu veya hata oluştu: " + ex2.Message);
		}
		finally
		{
			if (!string.IsNullOrEmpty(clientUsername) && clientUdpEndpoints.ContainsKey(clientUsername))
			{
				lock (clientTcpClients)
				{
					clientUdpEndpoints.Remove(clientUsername);
					clientTcpClients.Remove(clientUsername);
				}
				BroadcastUserList();
				BroadcastMessage(clientUsername + " sohbet odasından ayrıldı.");
			}
			stream.Close();
			tcpClient.Close();
		}
	}

	private static async Task StartUdpReceiver(int port)
	{
		try
		{
			udpServer = new UdpClient(port);
			Console.WriteLine("UDP alıcı başlatıldı.");
			while (true)
			{
				UdpReceiveResult receiveResult = await udpServer.ReceiveAsync();
				UdpPacket packet = new UdpPacket
				{
					Data = receiveResult.Buffer,
					Sender = receiveResult.RemoteEndPoint
				};
				udpPacketQueue.Add(packet);
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Console.WriteLine("UDP Alıcı Hatası: " + ex2.Message);
		}
	}

	private static async Task ProcessUdpQueue(CancellationToken cancellationToken)
	{
		Console.WriteLine("UDP işleme kuyruğu başlatıldı.");
		try
		{
			foreach (UdpPacket packet in udpPacketQueue.GetConsumingEnumerable(cancellationToken))
			{
				List<Task> tasks = new List<Task>();
				foreach (KeyValuePair<string, IPEndPoint> entry in clientUdpEndpoints.Where((KeyValuePair<string, IPEndPoint> e) => !e.Value.Equals(packet.Sender)))
				{
					tasks.Add(udpServer.SendAsync(packet.Data, packet.Data.Length, entry.Value));
				}
				Task.WhenAll(tasks).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch (OperationCanceledException)
		{
			Console.WriteLine("UDP işleme kuyruğu durduruldu.");
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
			Console.WriteLine("UDP İşleme Kuyruğu Hatası: " + ex3.Message);
		}
	}

	private static void BroadcastMessage(string message)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(message);
		lock (clientTcpClients)
		{
			foreach (TcpClient value in clientTcpClients.Values)
			{
				try
				{
					if (value.Connected)
					{
						NetworkStream stream = value.GetStream();
						stream.Write(bytes, 0, bytes.Length);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("Mesaj gönderme hatası: " + ex.Message);
				}
			}
		}
	}

	private static void BroadcastUserList()
	{
		string message = "USERLIST:" + string.Join(",", clientUdpEndpoints.Keys);
		BroadcastMessage(message);
	}
}
