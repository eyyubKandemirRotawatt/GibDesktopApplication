using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GibDesktopApplication
{
    // Modern Card Panel with shadow and rounded corners
    public class ModernCard : Panel
    {
        private int borderRadius = 12;
        private Color shadowColor = Color.FromArgb(30, 0, 0, 0);
        private int shadowSize = 8;
        private Color borderColor = Color.FromArgb(220, 220, 220);

        public ModernCard()
        {
            this.DoubleBuffered = true;
            this.Padding = new Padding(15);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw shadow
            using (GraphicsPath shadowPath = GetRoundedRectPath(new Rectangle(shadowSize, shadowSize,
                Width - shadowSize, Height - shadowSize), borderRadius))
            {
                using (PathGradientBrush shadowBrush = new PathGradientBrush(shadowPath))
                {
                    shadowBrush.CenterColor = shadowColor;
                    shadowBrush.SurroundColors = new[] { Color.Transparent };
                    e.Graphics.FillPath(shadowBrush, shadowPath);
                }
            }

            // Draw card background
            using (GraphicsPath path = GetRoundedRectPath(new Rectangle(0, 0, Width - shadowSize, Height - shadowSize), borderRadius))
            {
                e.Graphics.FillPath(new SolidBrush(this.BackColor), path);

                // Draw subtle border
                using (Pen borderPen = new Pen(borderColor, 1))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            }
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float r = radius;

            path.AddArc(rect.X, rect.Y, r, r, 180, 90);
            path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
            path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
            path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
            path.CloseFigure();

            return path;
        }
    }

    // Modern Button with gradient and hover effects
    public class ModernButton : Button
    {
        private Color baseColor;
        private Color hoverColor;
        private bool isHovered = false;
        private int borderRadius = 8;

        public ModernButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Cursor = Cursors.Hand;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        }

        public void SetColors(Color baseColor, Color hoverColor)
        {
            this.baseColor = baseColor;
            this.hoverColor = hoverColor;
            this.BackColor = baseColor;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            if (this.Enabled)
            {
                this.BackColor = hoverColor;
                this.Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            if (this.Enabled)
            {
                this.BackColor = baseColor;
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Add subtle gradient on hover
            if (isHovered && this.Enabled)
            {
                using (GraphicsPath path = GetRoundedRectPath(new Rectangle(0, 0, Width, Height), borderRadius))
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        this.ClientRectangle,
                        Color.FromArgb(20, 255, 255, 255),
                        Color.Transparent,
                        LinearGradientMode.Vertical))
                    {
                        pevent.Graphics.FillPath(brush, path);
                    }
                }
            }
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float r = radius;

            path.AddArc(rect.X, rect.Y, r, r, 180, 90);
            path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
            path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
            path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
            path.CloseFigure();

            return path;
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            if (!this.Enabled)
            {
                this.BackColor = Color.FromArgb(200, 200, 200);
            }
            else
            {
                this.BackColor = baseColor;
            }
        }
    }

    // Modern TextBox with focus effects
    public class ModernTextBox : TextBox
    {
        private Color focusColor = Color.FromArgb(52, 152, 219);
        private Color normalColor = Color.FromArgb(220, 220, 220);
        private bool isFocused = false;

        public ModernTextBox()
        {
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Font = new Font("Segoe UI", 9.5F);
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            isFocused = true;
            this.Invalidate();
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            isFocused = false;
            this.Invalidate();
        }
    }

    // Section Header Label
    public class SectionHeader : Label
    {
        public SectionHeader()
        {
            this.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            this.ForeColor = Color.FromArgb(44, 62, 80);
            this.AutoSize = true;
        }
    }
}
