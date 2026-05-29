using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MarkdownViewer.Core
{
    static class Icons
    {
        static Image eyeIconCached = null;
        static Image editIconCached = null;

        public static Image GetEyeIcon()
        {
            if (eyeIconCached == null) eyeIconCached = CreateEyeIcon();
            return eyeIconCached;
        }

        public static Image GetEditIcon()
        {
            if (editIconCached == null) editIconCached = CreateEditIcon();
            return editIconCached;
        }

        public static Image CreateEyeIcon()
        {
            Bitmap bmp = new Bitmap(24, 24);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                GraphicsPath roundRect = CreateRoundRectPath(2, 5, 20, 14, 4);
                using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                    new Rectangle(2, 5, 20, 14),
                    Color.FromArgb(200, 220, 255),
                    Color.FromArgb(100, 150, 220),
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(bgBrush, roundRect);
                }

                using (Pen borderPen = new Pen(Color.FromArgb(80, 120, 180), 1))
                {
                    g.DrawPath(borderPen, roundRect);
                }

                using (LinearGradientBrush eyeBgBrush = new LinearGradientBrush(
                    new Rectangle(7, 8, 10, 8),
                    Color.FromArgb(230, 240, 255),
                    Color.FromArgb(150, 190, 240),
                    LinearGradientMode.Vertical))
                {
                    g.FillEllipse(eyeBgBrush, 7, 8, 10, 8);
                }

                using (Pen eyeBorderPen = new Pen(Color.FromArgb(60, 100, 160), 1))
                {
                    g.DrawEllipse(eyeBorderPen, 7, 8, 10, 8);
                }

                using (SolidBrush pupilBrush = new SolidBrush(Color.FromArgb(80, 120, 180)))
                {
                    g.FillEllipse(pupilBrush, 10, 10, 4, 4);
                }

                using (SolidBrush highlightBrush = new SolidBrush(Color.FromArgb(180, 220, 255)))
                {
                    g.FillEllipse(highlightBrush, 8, 9, 2, 2);
                }

                using (LinearGradientBrush glossBrush = new LinearGradientBrush(
                    new Rectangle(3, 6, 18, 6),
                    Color.FromArgb(80, Color.White),
                    Color.FromArgb(0, Color.White),
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(glossBrush, roundRect);
                }
            }
            return bmp;
        }

        public static Image CreateEditIcon()
        {
            Bitmap bmp = new Bitmap(24, 24);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                GraphicsPath roundRect = CreateRoundRectPath(2, 2, 20, 20, 5);
                using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                    new Rectangle(2, 2, 20, 20),
                    Color.FromArgb(220, 235, 255),
                    Color.FromArgb(120, 170, 230),
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(bgBrush, roundRect);
                }

                using (Pen borderPen = new Pen(Color.FromArgb(80, 130, 190), 1))
                {
                    g.DrawPath(borderPen, roundRect);
                }

                using (LinearGradientBrush lineBrush = new LinearGradientBrush(
                    new Rectangle(5, 6, 14, 3),
                    Color.FromArgb(255, 255, 255),
                    Color.FromArgb(200, 220, 250),
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(lineBrush, 5, 6, 14, 3);
                    g.FillRectangle(lineBrush, 5, 11, 12, 3);
                    g.FillRectangle(lineBrush, 5, 16, 14, 3);
                }

                using (LinearGradientBrush glossBrush = new LinearGradientBrush(
                    new Rectangle(3, 3, 18, 8),
                    Color.FromArgb(60, Color.White),
                    Color.FromArgb(0, Color.White),
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(glossBrush, roundRect);
                }
            }
            return bmp;
        }

        static GraphicsPath CreateRoundRectPath(int x, int y, int width, int height, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int right = x + width;
            int bottom = y + height;

            path.AddLine(x + radius, y, right - radius, y);
            path.AddArc(right - radius * 2, y, radius * 2, radius * 2, 270, 90);

            path.AddLine(right, y + radius, right, bottom - radius);
            path.AddArc(right - radius * 2, bottom - radius * 2, radius * 2, radius * 2, 0, 90);

            path.AddLine(right - radius, bottom, x + radius, bottom);
            path.AddArc(x, bottom - radius * 2, radius * 2, radius * 2, 90, 90);

            path.AddLine(x, bottom - radius, x, y + radius);
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);

            path.CloseFigure();
            return path;
        }
    }
}
