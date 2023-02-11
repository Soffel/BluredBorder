using BlurryBitmap;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace BlurredBorder
{
    public partial class Form1 : Form
    {

        private Bitmap _orgImage;
        private Size _editSize;
        private string _imagepath;

        private readonly string[] _formats = new string[] { "Quadratisch", "Hochformat", "Querformat", "Story" };

        public Form1()
        {
            InitializeComponent();

            button1.Enabled = false;
            button2.Enabled = false;

            Format.Items.AddRange(_formats);
            Format.SelectedIndex = 0;
        }

        private void panel1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop, false) is string[] drop)
            {
                ImageName.Text = drop[0];
                _imagepath = Path.GetDirectoryName(ImageName.Text);
                _orgImage = new Bitmap(ImageName.Text);

                if (_orgImage != default)
                {
                   var show = ScaleImage(_orgImage.Clone() as Image, pictureBox1.Size);

                    if(show != null)
                    {
                        pictureBox1.Image = show.Clone() as Bitmap;
                        pictureBox2.Image = show.Clone() as Bitmap;

                        button1.Enabled = true;
                        button2.Enabled = true;
                    }     
                }
                   
            }
        }

        private void panel1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private Bitmap? ScaleImage(Image image, Size size)
        {
            if (image == default)
                return null;

            var scaleWidth = size.Width / (double)image.Width;
            var scaleHeight = size.Height / (double)image.Height;

            var sizeFactor = (scaleHeight > scaleWidth) ? scaleWidth : scaleHeight;

            var newHeigth = sizeFactor * image.Height;
            var newWidth = sizeFactor * image.Width;

            var newImage = new Bitmap((int)newWidth, (int)newHeigth);
            using (var g = Graphics.FromImage(newImage))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, new Rectangle(0, 0, (int)newWidth, (int)newHeigth));
            }

            return newImage;       
        }

        private async void button1_ClickAsync(object sender, EventArgs e)
        {
            if (_orgImage == default)
                return;

            if(int.TryParse(textBox1.Text, out int blur))
            {
                var blurEffect = new BlurEffect();
                button1.Enabled = false;
                button2.Enabled = false;

                if(_orgImage.Clone() is Bitmap image)
                {
                    await blurEffect.ApplyAsync(image, blur);

                    var result = MergeImage(image, _orgImage, _editSize);

                    pictureBox2.Image = ScaleImage(result, pictureBox2.Size);
                }
               
  
                button1.Enabled = true;
                button2.Enabled = true;
            }
          
        }

        private Bitmap MergeImage(Image Background, Image Foreground, Size finalSize)
        {
            Bitmap bitmap = new Bitmap(finalSize.Width, finalSize.Height);

            var background = new Bitmap(ResizeImage(Background, finalSize));
            var foreground = ScaleImage(Foreground, finalSize);
            var x = foreground.Width == finalSize.Width ? 0 : ((finalSize.Width - foreground.Width) / 2);
            var y = foreground.Height == finalSize.Height ? 0 : ((finalSize.Height - foreground.Height) / 2);


            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.DrawImage(background, 0, 0);
                g.DrawImage(foreground,x,y);
            }
            Image img = bitmap;

            img.Save(_imagepath + "\\test.jpg");
            return bitmap;
        }


        public static Image ResizeImage(Image imgToResize, Size destinationSize)
        {
            var originalWidth = imgToResize.Width;
            var originalHeight = imgToResize.Height;

            //how many units are there to make the original length
            var hRatio = (float)originalHeight / destinationSize.Height;
            var wRatio = (float)originalWidth / destinationSize.Width;

            //get the shorter side
            var ratio = Math.Min(hRatio, wRatio);

            var hScale = Convert.ToInt32(destinationSize.Height * ratio);
            var wScale = Convert.ToInt32(destinationSize.Width * ratio);

            //start cropping from the center
            var startX = (originalWidth - wScale) / 2;
            var startY = (originalHeight - hScale) / 2;

            //crop the image from the specified location and size
            var sourceRectangle = new Rectangle(startX, startY, wScale, hScale);

            //the future size of the image
            var bitmap = new Bitmap(destinationSize.Width, destinationSize.Height);

            //fill-in the whole bitmap
            var destinationRectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

            //generate the new image
            using (var g = Graphics.FromImage(bitmap))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(imgToResize, destinationRectangle, sourceRectangle, GraphicsUnit.Pixel);
            }

            return bitmap;

        }


        private void button2_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = default;
            _orgImage.Dispose();
            button1.Enabled = false;
            ImageName.Text = "Drop Image...";
        }

        private void Format_SelectedIndexChanged(object sender, EventArgs e)
        {
            resolution.Items.Clear();
            List<string> formats = new();
            switch (Format.SelectedIndex)
            {
                case 0:
                    {
                        formats.Add("1080 x 1080 px");
                        _editSize = new Size(1080, 1080);
                    }
                    break;
                case 1:
                    {
                        formats.Add("1080 x 1350 px");
                        _editSize = new Size(1080, 1350);
                    }      
                    break;
                case 2:
                    {
                        formats.Add("1200 x 628 px");
                        _editSize = new Size(1200, 628);
                    }
                    break;
                case 3:
                    {
                        formats.Add("1080 x 1920 px");
                        _editSize = new Size(1080, 1920);
                    }
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }
            resolution.Items.AddRange(formats.ToArray());
            resolution.SelectedIndex = 0;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            switch (Format.SelectedIndex)
            {
                case 0:
                    {
                        switch (resolution.SelectedIndex)
                        {
                            case 0:
                                {
                                    _editSize = new Size(1080, 1080);
                                }
                                break;
                        }
                    }
                    break;
                case 1:
                    {
                        switch (resolution.SelectedIndex)
                        {
                            case 0:
                                {
                                    _editSize = new Size(1080, 1350);
                                }
                                break;
                        }   
                    }
                    break;
                case 2:
                    {
                        switch (resolution.SelectedIndex)
                        {
                            case 0:
                                {
                                    _editSize = new Size(1200, 628);
                                }
                                break;
                        }        
                    }
                    break;
                case 3:
                    {
                        switch (resolution.SelectedIndex)
                        {
                            case 0:
                                {
                                    _editSize = new Size(1080, 1920);
                                }
                                break;
                        }
                    }
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            textBox1.Text = trackBar1.Value.ToString();
        }
    }
}