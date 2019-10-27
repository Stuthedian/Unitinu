using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using System.Net;
using System.IO;

namespace SaS
{
    public partial class Form1 : Form
    {
        Image image, originalimage, tempimage;
        Bitmap tempbitmap;
        bool mirrored = false;
        Point startpoint, endpoint;
        Pen selectionpen = new Pen(Color.Gray)
        {
            DashStyle = DashStyle.Dash,
            Width = 4
        };
        Rectangle rectangle;
        Graphics graphics;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        { 
            копироватьToolStripMenuItem.Enabled = false;
            выделениеToolStripMenuItem.Enabled = false;
            отразитьToolStripMenuItem.Enabled = false;
            toolStripComboBox1.ComboBox.ValueMember = "Value";
            toolStripComboBox1.ComboBox.DisplayMember = "Text";
            foreach (ToolStripItem item in toolStrip1.Items)
            {
                item.Enabled = false;
            }
            trackBar1.Enabled = false;
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            if (openFileDialog1.FileName == "")
                return;
            try
            {
                image = Image.FromFile(openFileDialog1.FileName);
                originalimage = (Image)image.Clone();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\nНе удалось открыть файл", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                openFileDialog1.FileName = "";
                return;
            }
            process_image();
        }
        
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if(trackBar1.Value != 0)
            {
                Rectangle rectangle = new Rectangle(-1, 0, trackBar1.Value, image.Height);
                Bitmap bitmap1 = new Bitmap(image);
                if (mirrored)
                    rectangle.X = image.Width - trackBar1.Value;
                else
                    rectangle.X = 0;
                bitmap1 = bitmap1.Clone(rectangle, image.PixelFormat);
                Bitmap emptybit = new Bitmap(bitmap1.Width * 2, image.Height);
                if (mirrored)
                    bitmap1.RotateFlip(RotateFlipType.Rotate180FlipY);
                Graphics.FromImage(emptybit).DrawImage(bitmap1, 0, 0);
                bitmap1.RotateFlip(RotateFlipType.Rotate180FlipY);
                Graphics.FromImage(emptybit).DrawImage(bitmap1, bitmap1.Width, 0);
                pictureBox1.Image = emptybit;
                tempimage = pictureBox1.Image;
                GC.Collect();
            }
        }

        private void отразитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mirrored = !mirrored;
            trackBar1_Scroll(sender, e);
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBoxItem a = (ComboBoxItem)toolStripComboBox1.Items[toolStripComboBox1.SelectedIndex];
            resize_image(a.Value);
        }

        private void resize_image(double multiplier)
        {
            Rectangle destRect = new Rectangle(0, 0, Convert.ToInt32(originalimage.Width * multiplier),
                Convert.ToInt32(originalimage.Height * multiplier));
            Bitmap destImage = new Bitmap(destRect.Width, destRect.Height);
            destImage.SetResolution(originalimage.HorizontalResolution, originalimage.VerticalResolution);
            using (Graphics graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (ImageAttributes wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(originalimage, destRect, 0, 0, originalimage.Width, originalimage.Height, 
                        GraphicsUnit.Pixel, wrapMode);
                }
            }

            image = destImage;
            trackBar1.SetRange(0, image.Width);
            trackBar1.Value = image.Width / 2;
            trackBar1_Scroll(new object(), new EventArgs());
        }

        private void всёИзображениеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetImage(pictureBox1.Image);
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            startpoint.X = e.X;
            startpoint.Y = e.Y;
            rectangle = new Rectangle(startpoint.X, startpoint.Y, 0, 0);
            tempbitmap = new Bitmap(pictureBox1.Image.Width, pictureBox1.Image.Height);
            graphics = Graphics.FromImage(tempbitmap);
            pictureBox1.MouseMove += pictureBox1_MouseMove;
            pictureBox1.MouseUp += pictureBox1_MouseUp;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            endpoint.X = e.X;
            endpoint.Y = e.Y;
            rectangle.Width = Math.Abs(endpoint.X - rectangle.X);
            rectangle.Height = Math.Abs(endpoint.Y - rectangle.Y);
            graphics.DrawImage(tempimage, 0, 0);
            graphics.DrawRectangle(selectionpen, rectangle);
            pictureBox1.Image = tempbitmap;
        }

        private void выделениеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            graphics.DrawImage(tempimage, 0, 0);
            graphics.DrawRectangle(new Pen(Color.Transparent), rectangle);
            Bitmap bitmap = ((Bitmap)pictureBox1.Image).Clone(rectangle, pictureBox1.Image.PixelFormat);
            Clipboard.SetImage(bitmap);
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            pictureBox1.MouseMove -= pictureBox1_MouseMove;
        }

        private void вставитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Clipboard.GetImage() == null)
            {
                MessageBox.Show("Буфер обмена пуст, либо его содержимое не является изображением", "Вставка невозможна",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                image = Clipboard.GetImage();
                originalimage = (Image)image.Clone();
                process_image();
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if(SelectionButton1.Checked)
            {
                pictureBox1.Cursor = Cursors.Cross; 
                pictureBox1.MouseDown += pictureBox1_MouseDown;
                выделениеToolStripMenuItem.Enabled = true;
            }
            else
            {
                выделениеToolStripMenuItem.Enabled = false;
                pictureBox1.MouseDown -= pictureBox1_MouseDown;
                pictureBox1.Image = tempimage;
                pictureBox1.Cursor = Cursors.Default;
            }
        }

        private void process_image()
        {
            toolStripComboBox1.SelectedIndexChanged -= toolStripComboBox1_SelectedIndexChanged;
            toolStripComboBox1.ComboBox.DataSource = new ComboBoxItem[] {
                new ComboBoxItem{ Value = 0.5, Text = "50%" },
                new ComboBoxItem{ Value = 0.75, Text = "75%" },
                new ComboBoxItem{ Value = 1.0, Text = "100%" }
            };
            toolStripComboBox1.SelectedIndexChanged += toolStripComboBox1_SelectedIndexChanged;
            toolStripComboBox1.ComboBox.SelectedValue = 1.0;
            if (this.Width >= Screen.PrimaryScreen.WorkingArea.Width
                || this.Height >= Screen.PrimaryScreen.WorkingArea.Height)
            {
                double mulW = Screen.PrimaryScreen.WorkingArea.Width / (originalimage.Width * 1.0);
                double mulH = Screen.PrimaryScreen.WorkingArea.Height / (originalimage.Height * 1.0);
                double result = Math.Round(mulW > mulH && mulW < 1.0 ? mulW * 0.7 : mulH * 0.7, 2);

                resize_image(result);
                var a = ((ComboBoxItem[])toolStripComboBox1.ComboBox.DataSource).ToList();
                a.Add(new ComboBoxItem()
                {
                    Value = result,
                    Text = Convert.ToInt32(result * 100).ToString() + "%"
                });
                toolStripComboBox1.SelectedIndexChanged -= toolStripComboBox1_SelectedIndexChanged;
                a = a.OrderBy(m => m.Value).ToList();
                var i = a.FindIndex(m => m.Value == result);
                toolStripComboBox1.ComboBox.DataSource = a.Take(i + 1).ToList();
                toolStripComboBox1.ComboBox.SelectedValue = result;
                toolStripComboBox1.SelectedIndexChanged += toolStripComboBox1_SelectedIndexChanged;
            }
            else
            {
                trackBar1.SetRange(0, image.Width);
                trackBar1.Value = image.Width / 2;
                trackBar1_Scroll(new object(), new EventArgs());
            }
            копироватьToolStripMenuItem.Enabled = true;
            отразитьToolStripMenuItem.Enabled = true;
            foreach (ToolStripItem item in toolStrip1.Items)
            {
                item.Enabled = true;
            }
            trackBar1.Enabled = true;
            Location = new Point(Screen.PrimaryScreen.WorkingArea.Width/2 - image.Width/2 , 
                Screen.PrimaryScreen.WorkingArea.Height/2 - image.Height/2);
        }

        private class ComboBoxItem
        {
            public double Value { get; set; }
            public string Text { get; set; }
        }
    }
}
