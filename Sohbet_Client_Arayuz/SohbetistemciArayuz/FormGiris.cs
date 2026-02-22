using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SohbetistemciArayuz;

public class FormGiris : Form
{
	private IContainer components = null;

	private Label label1;

	private TextBox txtKullaniciAdi;

	private Button btnTamam;

	private TextBox txtIpAdresi;

	private Label label2;

	public string KullaniciAdi { get; private set; }

	public string IpAdresi { get; private set; }

	public FormGiris()
	{
		InitializeComponent();
	}

	private void btnTamam_Click(object sender, EventArgs e)
	{
		if (!string.IsNullOrWhiteSpace(txtKullaniciAdi.Text) && !string.IsNullOrWhiteSpace(txtIpAdresi.Text))
		{
			KullaniciAdi = txtKullaniciAdi.Text.Trim();
			IpAdresi = txtIpAdresi.Text.Trim();
			base.DialogResult = DialogResult.OK;
			Close();
		}
		else
		{
			MessageBox.Show("Kullanıcı adı ve IP adresi boş bırakılamaz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SohbetistemciArayuz.FormGiris));
		this.label1 = new System.Windows.Forms.Label();
		this.txtKullaniciAdi = new System.Windows.Forms.TextBox();
		this.btnTamam = new System.Windows.Forms.Button();
		this.txtIpAdresi = new System.Windows.Forms.TextBox();
		this.label2 = new System.Windows.Forms.Label();
		base.SuspendLayout();
		this.label1.AutoSize = true;
		this.label1.BackColor = System.Drawing.SystemColors.Control;
		this.label1.Font = new System.Drawing.Font("Microsoft YaHei", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.Location = new System.Drawing.Point(38, 9);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(313, 27);
		this.label1.TabIndex = 0;
		this.label1.Text = "Lütfen kullanıcı adınızı giriniz:";
		this.txtKullaniciAdi.Location = new System.Drawing.Point(43, 39);
		this.txtKullaniciAdi.Name = "txtKullaniciAdi";
		this.txtKullaniciAdi.Size = new System.Drawing.Size(308, 22);
		this.txtKullaniciAdi.TabIndex = 1;
		this.btnTamam.Location = new System.Drawing.Point(147, 170);
		this.btnTamam.Name = "btnTamam";
		this.btnTamam.Size = new System.Drawing.Size(94, 27);
		this.btnTamam.TabIndex = 2;
		this.btnTamam.Text = "Tamam";
		this.btnTamam.UseVisualStyleBackColor = true;
		this.btnTamam.Click += new System.EventHandler(btnTamam_Click);
		this.txtIpAdresi.Location = new System.Drawing.Point(43, 125);
		this.txtIpAdresi.Name = "txtIpAdresi";
		this.txtIpAdresi.Size = new System.Drawing.Size(308, 22);
		this.txtIpAdresi.TabIndex = 3;
		this.label2.AutoSize = true;
		this.label2.BackColor = System.Drawing.SystemColors.Control;
		this.label2.Font = new System.Drawing.Font("Microsoft YaHei", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label2.Location = new System.Drawing.Point(142, 83);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(99, 27);
		this.label2.TabIndex = 4;
		this.label2.Text = "IP nedir?";
		base.AutoScaleDimensions = new System.Drawing.SizeF(8f, 16f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(382, 209);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.txtIpAdresi);
		base.Controls.Add(this.btnTamam);
		base.Controls.Add(this.txtKullaniciAdi);
		base.Controls.Add(this.label1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.Name = "FormGiris";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "FormGiris";
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
