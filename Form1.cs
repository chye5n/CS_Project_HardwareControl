using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Point = OpenCvSharp.Point;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        Image img;
        VideoCapture video = new VideoCapture(0);
        Mat frame = new Mat();

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)  //video
        {
            video.Open(0, VideoCaptureAPIs.ANY);
            
            while (Cv2.WaitKey(33) != 'q')
            {
                video.Read(frame);

                Bitmap p1 = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame));
                pictureBox1.Image = p1;
                var form = Application.OpenForms["Form1"];
                if (form == null)
                {
                    break;
                }
            }
            frame.Dispose();
            video.Release();
            Cv2.DestroyAllWindows();
        }

        private void button2_Click(object sender, EventArgs e)  //capture1
        {
            Bitmap capture = new Bitmap(this.pictureBox1.Width, pictureBox1.Height);
            Graphics capture_graphics = Graphics.FromImage(capture);
            var form = Application.OpenForms["Form1"];
            capture_graphics.CopyFromScreen(new System.Drawing.Point(form.Location.X + 3 + pictureBox1.Location.X, form.Location.Y + this.pictureBox1.Location.Y + 33), new System.Drawing.Point(0, 0), pictureBox1.Size);
            capture.Save("capture.jpeg");

            Mat src = new Mat("capture.jpeg");
            Mat white = new Mat();
            Mat dst = src.Clone();
            Cv2.Resize(src, dst, new OpenCvSharp.Size(400, 400));
            Point[][] contours;
            HierarchyIndex[] hierarchy;
            //160, 80, 90
            Cv2.InRange(dst, new Scalar(100, 100, 100), new Scalar(255, 255, 255), white);
            Cv2.FindContours(white, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxTC89KCOS);

            List<Point[]> new_contours = new List<Point[]>();
            foreach (Point[] p in contours)
            {
                double length = Cv2.ArcLength(p, true);
                if (length > 10)
                {
                    new_contours.Add(p);
                }
            }

            Cv2.DrawContours(dst, new_contours, -1, new Scalar(180, 255, 255), 2, LineTypes.AntiAlias, null, 1);
            Bitmap p2 = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(dst));
            pictureBox2.Image = p2;
            Cv2.WaitKey(0);
            /*
            Bitmap p2 = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame));
            pictureBox2.Image = p2;*/
        }

        private void button3_Click(object sender, EventArgs e)  //open
        {
            openFileDialog1.InitialDirectory = @"C:\";
            openFileDialog1.Filter = "모든파일(*.*)|*.*";
            openFileDialog1.Title = "이미지 열기";
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            //img = new Bitmap(openFileDialog1.FileName);
            //pictureBox2.Image = img;

            Mat src = new Mat(openFileDialog1.FileName);
            Mat white = new Mat();
            Mat dst = src.Clone();
            Cv2.Resize(src, dst, new OpenCvSharp.Size(400, 400));
            Point[][] contours;
            HierarchyIndex[] hierarchy;
            //160, 80, 90
            Cv2.InRange(dst, new Scalar(100, 100, 100), new Scalar(255, 255, 255), white);
            Cv2.FindContours(white, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxTC89KCOS);

            List<Point[]> new_contours = new List<Point[]>();
            foreach (Point[] p in contours)
            {
                double length = Cv2.ArcLength(p, true);
                if (length > 10)
                {
                    new_contours.Add(p);
                }
            }

            Cv2.DrawContours(dst, new_contours, -1, new Scalar(180, 255, 255), 2, LineTypes.AntiAlias, null, 1);
            Bitmap p2 = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(dst));
            pictureBox2.Image = p2;
        }

        private Bitmap RoateImage(Bitmap src, float angle)
        {
            Bitmap trg = new Bitmap(src.Width, src.Height);

            Graphics g = Graphics.FromImage(trg);
            // 이미지 중심을 (0,0)으로 이동
            g.TranslateTransform(src.Width / 2, src.Height / 2);
            // 회전
            g.RotateTransform(angle);
            // 이미지 중심 원래 자표로 이동
            g.TranslateTransform(-src.Width / 2, -src.Height / 2);
            // 원본 이미지로 그리기
            g.DrawImage(src, new System.Drawing.Point(0, 0));

            return trg;
        }

        private void button4_Click(object sender, EventArgs e)  //capture2
        {
            Bitmap p3 = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame));
            pictureBox3.Image = p3;
        }

        private void button5_Click(object sender, EventArgs e)  //save
        {
            string fileName;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Title = "저장 할 위치";
            saveFileDialog1.OverwritePrompt = true;
            saveFileDialog1.Filter = "JPEG File(*.jpg)|*.jpg |Bitmap File(*.bmp)|*.bmp |PNG File(*.png)|*.png";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                fileName = saveFileDialog1.FileName;
                pictureBox1.Image.Save(fileName);
            }
        }
    }
}