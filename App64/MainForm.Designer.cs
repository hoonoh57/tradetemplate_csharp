namespace App64
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainForm - 모든 UI는 SetupUI()에서 코드로 생성
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1600, 900);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.Name = "MainForm";
            this.Text = "TradeTemplate";
            this.ResumeLayout(false);
        }
    }
}