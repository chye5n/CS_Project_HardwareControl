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
using System.IO.Ports;
using OpenCvSharp.Extensions;
using Point = OpenCvSharp.Point;
using DirectShowLib;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        Image img;
        VideoCapture video1 = new VideoCapture(0);
        VideoCapture video2 = new VideoCapture(1);
        Mat frame1 = new Mat();
        Mat frame2 = new Mat();
        public List<DsDevice> cameraDevices; // 캠 리스트
        //private int selectDeviceIndex = -1;
        //public int SelectDeviceIndex { get => selectDeviceIndex; set => SetMainUI(value); }

        private void GetCameraList() => cameraDevices.AddRange(from DsDevice dsDevice in DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice)
                                                               where !dsDevice.DevicePath.Contains("device:sw")
                                                               select dsDevice);
        public Form1()
        {
            InitializeComponent();

            cameraDevices = new List<DsDevice>();

            GetCameraList();

            foreach (var cameraDevice in cameraDevices)
                comboBox1.Items.Add(cameraDevice.Name);
        }
       
        private void button1_Click(object sender, EventArgs e)  //camera1.video
        {
            //video1.Open(0, VideoCaptureAPIs.ANY);
            if(!video1.IsOpened())
            {
                MessageBox.Show("첫번째");
            }
            while (Cv2.WaitKey(33) != 'q')
            {
                video1.Read(frame1);

                Cv2.ImShow("video1", frame1);
                //Bitmap p1 = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame1));
                //pictureBox1.Image = p1;
                var form = Application.OpenForms["Form1"];
                if (form == null)
                {
                    break;
                }
            }
            frame1.Dispose();
            video1.Release();
        }

        private void button2_Click(object sender, EventArgs e)  //camera1.capture1
        {
            Bitmap p3 = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame1));
            pictureBox2.Image = p3;
        }

        private void button3_Click(object sender, EventArgs e)  //camera1.open
        {
            openFileDialog1.InitialDirectory = @"C:\";
            openFileDialog1.Filter = "모든파일(*.*)|*.*";
            openFileDialog1.Title = "이미지 열기";
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            //img = Image.FromFile(openFileDialog1.FileName);
            //img = resizeImage(img);
            /*Mat src = new Mat(openFileDialog1.FileName);
            Bitmap bmp = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(src));

            pictureBox2.Image = resizeImage(bmp);

            
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

            pictureBox2.Image = resizeImage(p2);*/
            Mat src = new Mat(openFileDialog1.FileName);
            Mat gray = new Mat();
            Mat gaussian_blur = new Mat();
            Mat binary = new Mat();

            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            Cv2.GaussianBlur(gray, gaussian_blur, new OpenCvSharp.Size(21, 21), 1, 1, BorderTypes.Default);
            Cv2.Threshold(gray, binary, 200, 255, ThresholdTypes.Binary);
            Bitmap p1 = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(gray));
            Bitmap p2 = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(gaussian_blur));
            Bitmap p3 = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binary));

            pictureBox1.Image = p1;
            pictureBox2.Image = p2;
            pictureBox3.Image = p3;
        }
        /*
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

            pictureBox2.Image = resizeImage(p2);
        }
        */
        public static Image resizeImage(Image image)
        {
            if (image != null)
            {
                Bitmap croppedBitmap = new Bitmap(image);
                croppedBitmap = croppedBitmap.Clone(
                        new Rectangle(30, 30, image.Width - 50, image.Height - 50),
                        System.Drawing.Imaging.PixelFormat.DontCare);
                return croppedBitmap;
            }
            else
            {
                return image;
            }
        }

        private Bitmap RoateImage(Bitmap src, float angle)
        {
            Bitmap trg = new Bitmap(src.Width, src.Height);

            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(trg);
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

        private void button4_Click(object sender, EventArgs e)  //camera1.capture2
        {
            Bitmap capture = new Bitmap(this.pictureBox1.Width, pictureBox1.Height);
            Graphics capture_graphics = Graphics.FromImage(capture);
            var form = Application.OpenForms["Form1"];
            capture_graphics.CopyFromScreen(new System.Drawing.Point(form.Location.X + groupBox2.Location.X + 545, form.Location.Y + this.groupBox2.Location.Y + 45), new System.Drawing.Point(0, 0), pictureBox2.Size);
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
            pictureBox3.Image = p2;
            Cv2.WaitKey(0);
            /*
            Bitmap p2 = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame));
            pictureBox2.Image = p2;*/
        }

        private void button5_Click(object sender, EventArgs e)  //camera1.save
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

        private void button6_Click(object sender, EventArgs e)  //COM Port
        {

        }

        private void button7_Click(object sender, EventArgs e)  //camera2.video
        {
            //video.Open(0, VideoCaptureAPIs.ANY);
            if (!video2.IsOpened())
            {
                MessageBox.Show("두번째");
            }
            while (Cv2.WaitKey(33) != 'q')
            {
                video2.Read(frame2);

                Cv2.ImShow("video2", frame2);
                //Bitmap p4 = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame2));
                //pictureBox4.Image = p4;
                var form = Application.OpenForms["Form1"];
                if (form == null)
                {
                    break;
                }
            }
            frame2.Dispose();
            video2.Release();
        }

        private void button11_Click(object sender, EventArgs e) //camera2.capture1
        {
            Bitmap p5 = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame2));
            pictureBox5.Image = p5;
        }

        private void button10_Click(object sender, EventArgs e) //camera2.capture2
        {
            Bitmap capture = new Bitmap(this.pictureBox5.Width, pictureBox5.Height);
            Graphics capture_graphics = Graphics.FromImage(capture);
            var form = Application.OpenForms["Form1"];
            capture_graphics.CopyFromScreen(new System.Drawing.Point(form.Location.X + groupBox3.Location.X + 545, form.Location.Y + this.groupBox3.Location.Y + 45), new System.Drawing.Point(0, 0), pictureBox5.Size);
            capture.Save("capture.jpeg");

            Mat src2 = new Mat("capture.jpeg");
            Mat white2 = new Mat();
            Mat dst2 = src2.Clone();
            Cv2.Resize(src2, dst2, new OpenCvSharp.Size(400, 400));
            Point[][] contours2;
            HierarchyIndex[] hierarchy;
            //160, 80, 90
            Cv2.InRange(dst2, new Scalar(100, 100, 100), new Scalar(255, 255, 255), white2);
            Cv2.FindContours(white2, out contours2, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxTC89KCOS);

            List<Point[]> new_contours = new List<Point[]>();
            foreach (Point[] p in contours2)
            {
                double length = Cv2.ArcLength(p, true);
                if (length > 10)
                {
                    new_contours.Add(p);
                }
            }

            Cv2.DrawContours(dst2, new_contours, -1, new Scalar(180, 255, 255), 2, LineTypes.AntiAlias, null, 1);
            Bitmap p6 = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(dst2));
            pictureBox6.Image = p6;
            Cv2.WaitKey(0);
        }
    }
}