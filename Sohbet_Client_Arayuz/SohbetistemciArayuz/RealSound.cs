using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;

namespace SohbetistemciArayuz;

public class RealSound : Form
{
	private TcpClient tcpClient;

	private NetworkStream stream;

	private string userName;

	private string serverIp;

	private UdpClient udpClient;

	private WaveInEvent waveIn;

	private Dictionary<IPEndPoint, BufferedWaveProvider> audioProviders;

	private Dictionary<IPEndPoint, WaveOutEvent> audioPlayers;

	private bool isConnected = false;

	private bool isMicrophoneActive = false;

	private bool isHeadphonesActive = false;

	private IPEndPoint serverEndPoint;

	private CancellationTokenSource cts;

	private IContainer components = null;

	private ListBox lstSohbet;

	private TextBox txtMesaj;

	private Button btnGonder;

	private Button btnSes;

	private ListView lstKullanicilar;

	private ColumnHeader columnHeader1;

	private ColumnHeader columnHeader2;

	private ColumnHeader columnHeader3;

	private Button btnKulaklik;

	private Button btnMikrofon;

	public RealSound(string user, TcpClient client, NetworkStream netStream, string ip)
	{
		InitializeComponent();
		userName = user;
		tcpClient = client;
		stream = netStream;
		serverIp = ip;
		base.FormClosing += RealSound_FormClosing;
		audioProviders = new Dictionary<IPEndPoint, BufferedWaveProvider>();
		audioPlayers = new Dictionary<IPEndPoint, WaveOutEvent>();
	}

	private void RealSound_Load(object sender, EventArgs e)
	{
		ConnectToServer();
	}

	private void RealSound_FormClosing(object sender, FormClosingEventArgs e)
	{
		DisconnectFromServer();
	}

	private void btnSes_Click(object sender, EventArgs e)
	{
		if (isConnected)
		{
			DisconnectFromServer();
		}
		else
		{
			ConnectToServer();
		}
	}

	private void ConnectToServer()
	{
		try
		{
			if (tcpClient == null || !tcpClient.Connected)
			{
				tcpClient = new TcpClient(serverIp, 8888);
				stream = tcpClient.GetStream();
			}
			isConnected = true;
			Text = userName + " - RealSound Uygulaması";
			AddChatMessage("Sunucuya bağlandı.");
			int port = new Random().Next(9000, 10000);
			byte[] bytes = Encoding.UTF8.GetBytes(port.ToString());
			stream.Write(bytes, 0, bytes.Length);
			byte[] bytes2 = Encoding.UTF8.GetBytes(userName);
			stream.Write(bytes2, 0, bytes2.Length);
			udpClient = new UdpClient(port);
			serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), 8888);
			cts = new CancellationTokenSource();
			Task.Run(delegate
			{
				ReadMessages(cts.Token);
			});
			Task.Run(delegate
			{
				ListenForAudio(cts.Token);
			});
			StartAudio();
			btnSes.Text = "Bağlantıyı Kes";
		}
		catch (Exception ex)
		{
			MessageBox.Show("Bağlantı hatası: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			DisconnectFromServer();
		}
	}

	private void DisconnectFromServer()
	{
		if (!isConnected)
		{
			return;
		}
		try
		{
			if (cts != null)
			{
				cts.Cancel();
				cts.Dispose();
				cts = null;
			}
			StopAudio();
			if (stream != null)
			{
				stream.Close();
			}
			if (tcpClient != null)
			{
				tcpClient.Close();
			}
			if (udpClient != null)
			{
				udpClient.Close();
				udpClient.Dispose();
			}
			isConnected = false;
			AddChatMessage("Bağlantı kesildi.");
			btnSes.Text = "Bağlan";
		}
		catch (Exception ex)
		{
			MessageBox.Show("Bağlantı kesme hatası: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void StartAudio()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		isMicrophoneActive = true;
		isHeadphonesActive = true;
		waveIn = new WaveInEvent();
		waveIn.WaveFormat = new WaveFormat(44100, 16, 1);
		waveIn.BufferMilliseconds = 50;
		waveIn.DataAvailable += WaveIn_DataAvailable;
		waveIn.StartRecording();
		AddChatMessage("Sesli sohbet başlatıldı.");
		Task.Run(delegate
		{
			ListenForAudio(cts.Token);
		});
	}

	private void StopAudio()
	{
		if (waveIn != null)
		{
			waveIn.StopRecording();
			waveIn.Dispose();
			waveIn = null;
		}
		lock (audioPlayers)
		{
			foreach (WaveOutEvent value in audioPlayers.Values)
			{
				value.Stop();
				value.Dispose();
			}
			audioPlayers.Clear();
		}
		lock (audioProviders)
		{
			audioProviders.Clear();
		}
		AddChatMessage("Sesli sohbet durduruldu.");
	}

	private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
	{
		if (isConnected && isMicrophoneActive)
		{
			try
			{
				udpClient.Send(e.Buffer, e.BytesRecorded, serverEndPoint);
			}
			catch (Exception)
			{
			}
		}
	}

	private void ListenForAudio(CancellationToken cancellationToken)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected O, but got Unknown
		IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				byte[] array = udpClient.Receive(ref remoteEP);
				lock (audioProviders)
				{
					if (!audioProviders.ContainsKey(remoteEP))
					{
						BufferedWaveProvider val = new BufferedWaveProvider(waveIn.WaveFormat)
						{
							BufferLength = waveIn.WaveFormat.AverageBytesPerSecond,
							DiscardOnBufferOverflow = true
						};
						audioProviders[remoteEP] = val;
						WaveOutEvent val2 = new WaveOutEvent();
						val2.Init((IWaveProvider)(object)val);
						val2.Play();
						audioPlayers[remoteEP] = val2;
					}
					audioProviders[remoteEP].AddSamples(array, 0, array.Length);
				}
			}
			catch (SocketException ex)
			{
				if (ex.SocketErrorCode == SocketError.Interrupted || ex.SocketErrorCode == SocketError.OperationAborted)
				{
					break;
				}
				AddChatMessage("Ses alma hatası: " + ex.Message);
			}
			catch (Exception)
			{
			}
		}
		lock (audioPlayers)
		{
			foreach (WaveOutEvent value in audioPlayers.Values)
			{
				value.Stop();
				value.Dispose();
			}
			audioPlayers.Clear();
		}
		lock (audioProviders)
		{
			audioProviders.Clear();
		}
	}

	private void ReadMessages(CancellationToken cancellationToken)
	{
		byte[] array = new byte[1024];
		try
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				int num = stream.Read(array, 0, array.Length);
				if (num == 0)
				{
					break;
				}
				string text = Encoding.UTF8.GetString(array, 0, num);
				if (text.StartsWith("USERLIST:"))
				{
					string[] users = text.Substring("USERLIST:".Length).Split(',');
					UpdateUserList(users);
				}
				else
				{
					AddChatMessage(text);
				}
			}
		}
		catch (Exception)
		{
			AddChatMessage("Sunucuyla bağlantı kesildi.");
		}
	}

	private void AddChatMessage(string message)
	{
		if (lstSohbet.InvokeRequired)
		{
			lstSohbet.Invoke((Action)delegate
			{
				lstSohbet.Items.Add(message);
			});
		}
		else
		{
			lstSohbet.Items.Add(message);
		}
	}

	private void UpdateUserList(string[] users)
	{
		if (lstKullanicilar.InvokeRequired)
		{
			lstKullanicilar.Invoke((Action)delegate
			{
				lstKullanicilar.Items.Clear();
				string[] array2 = users;
				foreach (string text2 in array2)
				{
					lstKullanicilar.Items.Add(new ListViewItem(new string[3] { text2, "Açık", "Açık" }));
				}
			});
			return;
		}
		HashSet<string> currentUsers = new HashSet<string>(users);
		List<IPEndPoint> list = audioPlayers.Keys.Where((IPEndPoint ep) => !currentUsers.Contains(ep.ToString())).ToList();
		lock (audioPlayers)
		{
			foreach (IPEndPoint item in list)
			{
				if (audioPlayers.ContainsKey(item))
				{
					audioPlayers[item].Stop();
					audioPlayers[item].Dispose();
					audioPlayers.Remove(item);
				}
			}
		}
		lock (audioProviders)
		{
			foreach (IPEndPoint item2 in list)
			{
				audioProviders.Remove(item2);
			}
		}
		lstKullanicilar.Items.Clear();
		string[] array = users;
		foreach (string text in array)
		{
			lstKullanicilar.Items.Add(new ListViewItem(new string[3] { text, "Açık", "Açık" }));
		}
	}

	private void btnMikrofon_Click(object sender, EventArgs e)
	{
		isMicrophoneActive = !isMicrophoneActive;
		btnMikrofon.Text = (isMicrophoneActive ? "Mikrofonu Kapat" : "Mikrofonu Aç");
		UpdateDeviceStatus();
	}

	private void btnKulaklik_Click(object sender, EventArgs e)
	{
		isHeadphonesActive = !isHeadphonesActive;
		btnKulaklik.Text = (isHeadphonesActive ? "Kulaklığı Kapat" : "Kulaklığı Aç");
		UpdateDeviceStatus();
	}

	private void btnGonder_Click(object sender, EventArgs e)
	{
		if (isConnected)
		{
			string text = userName + ": " + txtMesaj.Text;
			byte[] bytes = Encoding.UTF8.GetBytes(text);
			stream.Write(bytes, 0, bytes.Length);
			AddChatMessage(text);
			txtMesaj.Clear();
		}
	}

	private void UpdateDeviceStatus()
	{
		foreach (ListViewItem ıtem in lstKullanicilar.Items)
		{
			if (ıtem.Text == userName)
			{
				ıtem.SubItems[1].Text = (isMicrophoneActive ? "Açık" : "Kapalı");
				ıtem.SubItems[2].Text = (isHeadphonesActive ? "Açık" : "Kapalı");
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SohbetistemciArayuz.RealSound));
		this.lstSohbet = new System.Windows.Forms.ListBox();
		this.txtMesaj = new System.Windows.Forms.TextBox();
		this.btnGonder = new System.Windows.Forms.Button();
		this.btnSes = new System.Windows.Forms.Button();
		this.lstKullanicilar = new System.Windows.Forms.ListView();
		this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
		this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
		this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
		this.btnKulaklik = new System.Windows.Forms.Button();
		this.btnMikrofon = new System.Windows.Forms.Button();
		base.SuspendLayout();
		this.lstSohbet.FormattingEnabled = true;
		this.lstSohbet.ItemHeight = 16;
		this.lstSohbet.Location = new System.Drawing.Point(690, 46);
		this.lstSohbet.Name = "lstSohbet";
		this.lstSohbet.Size = new System.Drawing.Size(244, 356);
		this.lstSohbet.TabIndex = 0;
		this.txtMesaj.Location = new System.Drawing.Point(690, 416);
		this.txtMesaj.Name = "txtMesaj";
		this.txtMesaj.Size = new System.Drawing.Size(244, 22);
		this.txtMesaj.TabIndex = 1;
		this.btnGonder.Location = new System.Drawing.Point(762, 463);
		this.btnGonder.Name = "btnGonder";
		this.btnGonder.Size = new System.Drawing.Size(112, 30);
		this.btnGonder.TabIndex = 2;
		this.btnGonder.Text = "Mesajı Gönder";
		this.btnGonder.UseVisualStyleBackColor = true;
		this.btnGonder.Click += new System.EventHandler(btnGonder_Click);
		this.btnSes.Location = new System.Drawing.Point(509, 46);
		this.btnSes.Name = "btnSes";
		this.btnSes.Size = new System.Drawing.Size(175, 63);
		this.btnSes.TabIndex = 3;
		this.btnSes.Text = "Sesli Sohbet'i Başlat";
		this.btnSes.UseVisualStyleBackColor = true;
		this.btnSes.Click += new System.EventHandler(btnSes_Click);
		this.lstKullanicilar.Columns.AddRange(new System.Windows.Forms.ColumnHeader[3] { this.columnHeader1, this.columnHeader2, this.columnHeader3 });
		this.lstKullanicilar.FullRowSelect = true;
		this.lstKullanicilar.GridLines = true;
		this.lstKullanicilar.HideSelection = false;
		this.lstKullanicilar.Location = new System.Drawing.Point(30, 46);
		this.lstKullanicilar.Name = "lstKullanicilar";
		this.lstKullanicilar.Size = new System.Drawing.Size(320, 392);
		this.lstKullanicilar.TabIndex = 4;
		this.lstKullanicilar.UseCompatibleStateImageBehavior = false;
		this.lstKullanicilar.View = System.Windows.Forms.View.Details;
		this.columnHeader1.Text = "Kullanıcı Adı";
		this.columnHeader1.Width = 116;
		this.columnHeader2.Text = "Mikrofon";
		this.columnHeader2.Width = 63;
		this.columnHeader3.Text = "Kulaklık";
		this.columnHeader3.Width = 58;
		this.btnKulaklik.Location = new System.Drawing.Point(30, 463);
		this.btnKulaklik.Name = "btnKulaklik";
		this.btnKulaklik.Size = new System.Drawing.Size(161, 30);
		this.btnKulaklik.TabIndex = 5;
		this.btnKulaklik.Text = "Kulaklık Kapat";
		this.btnKulaklik.UseVisualStyleBackColor = true;
		this.btnKulaklik.Click += new System.EventHandler(btnKulaklik_Click);
		this.btnMikrofon.Location = new System.Drawing.Point(192, 463);
		this.btnMikrofon.Name = "btnMikrofon";
		this.btnMikrofon.Size = new System.Drawing.Size(158, 30);
		this.btnMikrofon.TabIndex = 6;
		this.btnMikrofon.Text = "Mikrofon Kapat";
		this.btnMikrofon.UseVisualStyleBackColor = true;
		this.btnMikrofon.Click += new System.EventHandler(btnMikrofon_Click);
		base.AutoScaleDimensions = new System.Drawing.SizeF(8f, 16f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(944, 553);
		base.Controls.Add(this.btnMikrofon);
		base.Controls.Add(this.btnKulaklik);
		base.Controls.Add(this.lstKullanicilar);
		base.Controls.Add(this.btnSes);
		base.Controls.Add(this.btnGonder);
		base.Controls.Add(this.txtMesaj);
		base.Controls.Add(this.lstSohbet);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.Name = "RealSound";
		this.Text = "RealSound";
		base.Load += new System.EventHandler(RealSound_Load);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
