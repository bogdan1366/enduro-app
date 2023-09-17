using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace System.Windows.Forms
{

	public class VLabel : System.Windows.Forms.Label
	{

		private bool bFlip = true;

        public VLabel()
        {

        }

		public VLabel(string text)
		{
            this.Text = text;
            this.Font = new Font("Arial", 12, FontStyle.Bold); 
            this.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.BackColor = Color.Transparent;
            this.Dock = System.Windows.Forms.DockStyle.Fill;
		}


		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;

			StringFormat stringFormat = new StringFormat();
			stringFormat.Alignment = StringAlignment.Center;
			stringFormat.Trimming = StringTrimming.None;
			stringFormat.FormatFlags = StringFormatFlags.DirectionVertical;

			Brush textBrush = new SolidBrush(this.ForeColor);
		
			Matrix storedState = g.Transform;

			if (bFlip)
			{
				g.RotateTransform(180f);

				g.TranslateTransform(-ClientRectangle.Width, 
									 -ClientRectangle.Height);
			}

			g.DrawString(
				this.Text,
				this.Font,
				textBrush,
				ClientRectangle,
				stringFormat);

			g.Transform = storedState;
		}

		[Description("When this parameter is true the VLabel flips at 180 degrees."),Category("Appearance")]
		public bool Flip180
		{
			get
			{
				return bFlip;
			}
			set
			{
				bFlip = value;
				this.Invalidate();
			}
		}
	}

    public class TextLabel : System.Windows.Forms.Label
    {
        public TextLabel(string text, FontStyle style)
        {
            this.Text = text;
            this.BackColor = Color.Transparent;
            this.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.Font = new Font("Arial", 10, style);
            this.Dock = System.Windows.Forms.DockStyle.Fill;
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTTRANSPARENT = (-1);

            if (m.Msg == WM_NCHITTEST)
            {
                m.Result = (IntPtr)HTTRANSPARENT;
            }
            else
            {
                base.WndProc(ref m);
            }
        }
    }

}
