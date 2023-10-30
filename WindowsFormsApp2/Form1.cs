using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using DirectShowLib;
using OpenCvSharp;
using OpenCvSharp.Flann;
using Point = OpenCvSharp.Point;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        FolderBrowserDialog Wheretosave = new FolderBrowserDialog();

        VideoCapture cam1, cam2, cam3, cam4, cam5;  //각 카메라 선언

        Mat frame1, frame2, frame3, frame4, frame5; //카메라 영상을 저장할 공간 선언

        double Cam1_Check1_Value, Cam1_Check2_Value, Cam2_Check1_Value, Cam2_Check2_Value, Cam3_Check1_Value, Cam3_Check2_Value, Cam4_Check1_Value, Cam4_Check2_Value, Cam5_Check1_Value, Cam5_Check2_Value;    //이진화 이미지의 임계값 저장

        float[] center = new float[50]; //점의 중심 좌표 저장

        public List<DsDevice> cameraDevices = null; //Camera List
        int index1, index2, index3, index4, index5; //Camera 선택 번호
        int Cam1_AngleCheck1_cnt = 0, Cam1_AngleCheck2_cnt = 0, Cam2_AngleCheck1_cnt = 0, Cam2_AngleCheck2_cnt = 0, Cam3_AngleCheck1_cnt = 0, Cam3_AngleCheck2_cnt = 0, Cam4_AngleCheck1_cnt = 0, Cam4_AngleCheck2_cnt = 0, Cam5_AngleCheck1_cnt = 0, Cam5_AngleCheck2_cnt = 0; //카메라가 선택되어 있는지 CheckPoint가 선택되어 있는지 확인
        int Cam1_AngleCheck1_Value_cnt = 0, Cam1_AngleCheck2_Value_cnt = 0, Cam2_AngleCheck1_Value_cnt = 0, Cam2_AngleCheck2_Value_cnt = 0, Cam3_AngleCheck1_Value_cnt = 0, Cam3_AngleCheck2_Value_cnt = 0, Cam4_AngleCheck1_Value_cnt = 0, Cam4_AngleCheck2_Value_cnt = 0, Cam5_AngleCheck1_Value_cnt = 0, Cam5_AngleCheck2_Value_cnt = 0; //AngleCheck버튼이 눌렸는지 확인
        int Cam1_cnt1 = 0, Cam1_cnt2 = 0, Cam2_cnt1 = 0, Cam2_cnt2 = 0, Cam3_cnt1 = 0, Cam3_cnt2 = 0, Cam4_cnt1 = 0, Cam4_cnt2 = 0, Cam5_cnt1 = 0, Cam5_cnt2 = 0;  //원본 사진 선 없이 캡처
        readonly string picture_path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);  //캡처된 이미지 저장 위치
        int[] XY = new int[20];

        private void GetCameraList() => cameraDevices.AddRange(from DsDevice dsDevice in DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice)  //설치된 카메라 찾기
                                                               where !dsDevice.DevicePath.Contains("device:video")
                                                               select dsDevice);

        public Form1()
        {
            InitializeComponent();
            CamSelect1_cbb.SelectedIndex = 0;   //카메라를 없음으로 선택
            CamSelect2_cbb.SelectedIndex = 0;
            CamSelect3_cbb.SelectedIndex = 0;
            CamSelect4_cbb.SelectedIndex = 0;
            CamSelect5_cbb.SelectedIndex = 0;

            Wheretosave.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);    //기본 저장위치 바탕화면으로 설정
            txt_Wheretosave.Text = Wheretosave.SelectedPath;    //저장위치 textBox에 입력

            Cam1_X.Enabled = false; Cam1_Y.Enabled = false; Cam1_mag.Enabled = false;   //위치 이동, 배율조절 비활성화
            Cam2_X.Enabled = false; Cam2_Y.Enabled = false; Cam2_mag.Enabled = false;
            Cam3_X.Enabled = false; Cam3_Y.Enabled = false; Cam3_mag.Enabled = false;
            Cam4_X.Enabled = false; Cam4_Y.Enabled = false; Cam4_mag.Enabled = false;
            Cam5_X.Enabled = false; Cam5_Y.Enabled = false; Cam5_mag.Enabled = false;
        }

        public partial class AngleCheck //이미지 크기 변환, 각도 계산, 이진화, 선그리기
        {
            public static Bitmap Resize(Bitmap image) //Bitmap 이미지 pictueBox크기로 변환
            {
                System.Drawing.Size resize = new System.Drawing.Size(250, 180); //pictureBox Size
                Bitmap resize_image = new Bitmap(image, resize);    //이미지 크기 변환
                return resize_image;
            }

            public static string Degree(float x1, float y1, float x2, float y2) //원의 중심으로 선의 각도 계산
            {
                double x = (double)(x2 - x1);   //기울기 계산
                double y = (double)(y2 - y1);
                double radian = Math.Atan2(y, x) * (180 / Math.PI);   //각도 계산
                string degree = radian.ToString("0.00");    //각도 소수점 두번째 자리까지 출력
                return degree;
            }

            public static string Angle(double a, double b)    //각도
            {
                double degree = Math.Abs(a - b);    //(Check1의 각도 - Check2의 각도)의 절대값
                if (degree > 180) { degree -= 180; }  //각도가 180도가 넘을 경우 180 빼기
                return degree.ToString("0.00"); //각도를 소수점 두번째 자리까지 출력
            }

            public static Mat Binary(double thresh, string img)   //이미지 이진화
            {
                Mat src = new Mat(img); //이미지 src에 할당
                Mat binary = src.Clone();  //개체의 복사본 생성
                Mat gray = new Mat();   //그레이 스케일로 변화시켜 단일 채널로 변경하기 위한 공간
                Mat gaussian_blur = new Mat();  //가우시안 블러를 한 결과를 저장할 공간

                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY); //이미지를 흑백으로 변경
                Cv2.GaussianBlur(gray, gaussian_blur, new OpenCvSharp.Size(21, 21), 1, 1, BorderTypes.Default); //각 지점에 가우시안 커널을 적용해 합산한 후에 출력
                Cv2.Threshold(gaussian_blur, binary, thresh, 255, ThresholdTypes.Binary);   //임계값을 기준으로 이진화
                return binary;
            }

            public static (Mat, float, float, float, float) Pencircle(Mat dst)  //점 찍고, 중심 위치 계산
            {
                int cnt = 0;    //원의 중심좌표 값 저장값
                double[] px = new double[100];  //원의 좌표 x값
                double[] py = new double[100];  //원의 좌표 y값
                Mat white = new Mat();  //점을 찍을 새로운 Mat선언

                Cv2.InRange(dst, new Scalar(100, 100, 100), new Scalar(255, 255, 255), white);  //들어온 dst를 원하는 색의 픽셀 추출하여 white로 출력
                Cv2.FindContours(white, out Point[][] contours, out HierarchyIndex[] hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxTC89KCOS);    //윤곽선 검출 함수

                foreach (Point[] p in contours) //contours안에서 윤곽선 정보 찾기
                {
                    double length = Cv2.ArcLength(p, true); //p의 곡선 길이 계산
                    if (length < 60 || length > 120) continue;  //곡선의 길이가 60보다 작거나 120보다 클 경우에는 무시한다

                    Moments moments = Cv2.Moments(p, false);    //윤곽선의 0차부터 3차까지의 모멘트 계산
                    Cv2.Circle(dst, (int)(moments.M10 / moments.M00), (int)(moments.M01 / moments.M00), 5, Scalar.Gray, -1);    //원의 중심점 계산
                    px[cnt] = (moments.M10 / moments.M00);  //원의 중심의 x좌표 값 배열에 저장
                    py[cnt] = (moments.M01 / moments.M00);  //원의 중심의 y좌표 값 배열에 저장
                    cnt++;
                }
                return (dst, (float)px[0], (float)py[0], (float)px[1], (float)py[1]);   //이진화 이미지, 점 두개의 x,y 좌표
            }

            public static Bitmap Penline(Bitmap img, float x1, float y1, float x2, float y2)  //선 연결(x1, y1, x2, y2는 좌표값)
            {
                Graphics graphics = Graphics.FromImage(img);    //그리기 클래스로 호출
                Pen pen = new Pen(Color.Red, 3);    //그릴 선의 색과 굵기 선택
                graphics.DrawLine(pen, x1, y1, x2, y2); //(x1, y1)에서 (x2, y2)까지 선 그리기
                return img;
            }
        }

        public partial class Position   //이미지 위치 이동, 확대, 축소
        {
            public static int X(string x, Image img)   //X좌표
            {
                if (x == "-") { return -0; }    //'-'입력시 '-' 입력
                else if ((x == "") || (x == "X")) { return 0; } //입력이 없을 경우 0
                else { if ((Convert.ToInt32(x) >= 250) || (Convert.ToInt32(x) <= -img.Width)) { x = null; } }  //영상의 가로길이 보다 크거나 작을 경우 0
                return Convert.ToInt32(x);
            }

            public static int Y(string y, Image img)   //Y좌표
            {
                if (y == "-") { return -0; }    //'-'입력시 '-' 입력
                else if (y == "") { return 0; } //입력이 없을 경우 0
                else { if ((Convert.ToInt32(y) >= 180) || (Convert.ToInt32(y) <= -img.Height)) { y = null; } }  //영상의 세로길이 보다 크거나 작을 경우 0
                return Convert.ToInt32(y);
            }

            public static Bitmap Mag_Image(Bitmap image, int width, int height)    //이미지 확대, 축소
            {
                if((width == 0) || (height == 0)) { return image; }
                else 
                {
                    var destinationRect = new Rectangle(0, 0, width, height);   //확대한 이미지의 크기
                    var destinationImage = new Bitmap(width, height);   //저장되는 이미지
                    destinationImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);   //해상도 설정

                    using (var graphics = Graphics.FromImage(destinationImage))
                    {
                        graphics.CompositingMode = CompositingMode.SourceCopy;
                        graphics.CompositingQuality = CompositingQuality.HighQuality;

                        using (var wrapMode = new ImageAttributes())
                        {
                            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                            graphics.DrawImage(image, destinationRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                        }
                    }
                    return destinationImage;
                }
            }
        }

        public partial class Save   //이미지 캡처, 원본이미지 수정
        {
            public static string save1(DateTime dateTime) 
            { string date = string.Format("{0:yyMMdd_HHmmss}_camera1", dateTime);       return date; }  //카메라 화면 저장명
            
            public static string save2_1(DateTime dateTime)
            { string date = string.Format("{0:yyMMdd_HHmmss}_picture1", DateTime.Now);  return date; }  //Check1의 원본 이미지 저장명

            public static string save2_2(DateTime dateTime)
            { string date = string.Format("{0:yyMMdd_HHmmss}_check1", DateTime.Now);    return date; }  //Check1의 이진화 이미지 저장명

            public static string save3_1(DateTime dateTime)
            { string date = string.Format("{0:yyMMdd_HHmmss}_picture2", DateTime.Now);  return date; }  //Check2의 원본 이미지 저장명

            public static string save3_2(DateTime dateTime)
            { string date = string.Format("{0:yyMMdd_HHmmss}_check2", DateTime.Now);    return date; }  //Check2의 이진화 이미지 저장명

            public static Bitmap Image_save(int x, int y)   //이미지 캡처(x, y는 캡처 될 이미지 위치의 좌표값)
            {
                Bitmap bmp = new Bitmap(250, 180);  //이미지 크기 설정
                Graphics graphic = Graphics.FromImage(bmp); //캡처할 이미지 
                var form = Application.OpenForms["Form1"];  //Form1에 접근
                graphic.CopyFromScreen(new System.Drawing.Point(form.Location.X + x + 8, form.Location.Y + y + 31), new System.Drawing.Point(0, 0), new System.Drawing.Size(250, 180)); //캡처 할 위치 설정
                return bmp;
            }

            public static Bitmap Origin_line(Bitmap origin, int x, int y, float center1, float center2, float center3, float center4)   //x, y는 이미지 이동 거리, center는 원의 중심점 좌표
            { 
                int p_width = origin.Width + x, p_height = origin.Height + y;
                if (p_width > 250) { p_width = 250; }
                if (p_height > 180) { p_height = 180; }
                if ((x < 0) && (y < 0))    //x축과 y축의 값이 모두 음수일 경우 이미지 자르기
                {
                    if (((250 - x) > p_width) && ((180 - y) <= p_height)) { origin = origin.Clone(new Rectangle(-x, -y, p_width, p_height), System.Drawing.Imaging.PixelFormat.DontCare); }
                    else if(((250 - x) <= p_width) && ((180 - y) > p_height)) { origin = origin.Clone(new Rectangle(-x, -y, p_width, p_height), System.Drawing.Imaging.PixelFormat.DontCare); }
                    else if(((250 - x) > p_width) && ((180 - y) > p_height)) { origin = origin.Clone(new Rectangle(-x, -y, p_width, p_height), System.Drawing.Imaging.PixelFormat.DontCare); }
                    else { origin = origin.Clone(new Rectangle(-x, -y, p_width, p_height), System.Drawing.Imaging.PixelFormat.DontCare); }
                }
                else if ((x < 0) && (y >= 0))    //x축 이동 값이 음수일 경우 이미지 자르기
                {
                    if(p_height < 180) { origin = origin.Clone(new Rectangle(-x, 0, p_width, origin.Height), System.Drawing.Imaging.PixelFormat.DontCare); }
                    else { origin = origin.Clone(new Rectangle(-x, 0, p_width, 180 - y), System.Drawing.Imaging.PixelFormat.DontCare); }
                }
                else if ((x >= 0) && (y < 0))    //y축 이동 값이 음수일 경우 이미지 자르기
                {
                    if(p_width < 250) { origin = origin.Clone(new Rectangle(0, -y, origin.Width, p_height), System.Drawing.Imaging.PixelFormat.DontCare); }
                    else { origin = origin.Clone(new Rectangle(0, -y, 250 - x, p_height), System.Drawing.Imaging.PixelFormat.DontCare); }
                }
                else    //x축과 y축의 값이 모두 양수일 경우 이미지 자르기
                {
                    if((p_width < 250) && (p_height < 180)) { origin = origin.Clone(new Rectangle(0, 0, origin.Width, origin.Height), System.Drawing.Imaging.PixelFormat.DontCare); }
                    else if(p_width < 250) { origin = origin.Clone(new Rectangle(0, 0, origin.Width, 180 - y), System.Drawing.Imaging.PixelFormat.DontCare); }
                    else if(p_height < 180) { origin = origin.Clone(new Rectangle(0, 0, 250 - x, origin.Height), System.Drawing.Imaging.PixelFormat.DontCare); }
                    else { origin = origin.Clone(new Rectangle(0, 0, 250 - x, 180 - y), System.Drawing.Imaging.PixelFormat.DontCare); } 
                }

                if ((x < 0) && (y >= 0)) { origin = AngleCheck.Penline(origin, center1, center2 - y, center3, center4 - y); }       //두 점으로 선 그리기
                else if ((x >= 0) && (y < 0)) { origin = AngleCheck.Penline(origin, center1 - x, center2, center3 - x, center4); }
                else if ((x < 0) && (y < 0)) { origin = AngleCheck.Penline(origin, center1, center2, center3, center4); }
                else { origin = AngleCheck.Penline(origin, center1 - x, center2 - y, center3 - x, center4 - y); }
                return origin;
            }
        }

        public partial class TXT    //입력받은 값 int, double형으로 변환
        {
            public static int TXT_to_INT(string txt)    //string을 int로 변환
            {
                if ((txt == "") || (txt == "-")) { return 0; }
                else
                {
                    int toint = Convert.ToInt32(txt);
                    return toint;
                }
            }

            public static double TXT_to_Double(string txt)  //string을 double로 변환
            {
                if ((txt == "") || (txt == "-")) { return 0; }
                else
                {
                    double toint = Convert.ToDouble(txt);
                    return toint;
                }
            }
        }

        private void Cam1_AngleCheck1_Click(object sender, EventArgs e) //1.Angle Check1
        { 
            if (Cam1_AngleCheck1_cnt == 1)  //Enter가 눌린 경우
            {
                center[0] = center[1] = center[2] = center[3] = 0;  //원의 위치 초기화
                Cam1_Open();
                Bitmap bmp = Save.Image_save(CheckPoint1group.Location.X + Cam1_Camera_pn.Location.X, CheckPoint1group.Location.Y + Cam1_Camera_pn.Location.Y);  //영상 캡처
                bmp.Save("resize_Cam1_Check1_pb.jpeg");  //캡처한 이미지 저장
            }
            else if (Cam1_AngleCheck1_cnt == 2) //Check Point1이 눌린 경우
            {
                Bitmap bmp = new Bitmap(Cam1_Check1_openFile.FileName); //Check Point1에서 불러온 이미지 Bitmap 변환
                AngleCheck.Resize(bmp).Save("resize_Cam1_Check1_pb.jpeg");  //이미지 크기 변환, 저장
            }
            Cam1_AngleCheck1_Value_cnt = 1; //Angle Check버튼 활성화

            Mat binary = AngleCheck.Binary(Cam1_Check1_Value, "resize_Cam1_Check1_pb.jpeg");    //저장된 이미지 이진화
            binary = AngleCheck.Pencircle(binary).Item1;    //이진화 이미지에 점찍기
            center[0] = AngleCheck.Pencircle(binary).Item2; center[1] = AngleCheck.Pencircle(binary).Item3; center[2] = AngleCheck.Pencircle(binary).Item4; center[3] = AngleCheck.Pencircle(binary).Item5; //찍은 점의 좌표 저장
            Cam1_Check1_pb.Image = AngleCheck.Penline(new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binary)), center[0], center[1], center[2], center[3]); //두 점으로 선 그리기
            Cam1_Check1_degree.Text = AngleCheck.Degree(center[0], center[1], center[2], center[3]);    //선의 각도
            XY[0] = decimal.ToInt32(Cam1_X.Value); XY[1] = decimal.ToInt32(Cam1_Y.Value);   //이동한 값 저장
            Cam1_cnt1 = 1;
        }

        private void Cam1_AngleCheck2_Click(object sender, EventArgs e) //1.Angle Check2
        {
            if (Cam1_AngleCheck2_cnt == 1)  //Enter가 눌린 경우
            {
                center[5] = center[6] = center[7] = center[8] = 0;  //원의 위치 초기화
                Cam1_Open();
                Bitmap bmp = Save.Image_save(CheckPoint1group.Location.X + Cam1_Camera_pn.Location.X, CheckPoint1group.Location.Y + Cam1_Camera_pn.Location.Y);  //영상 캡처
                bmp.Save("resize_Cam1_Check2_pb.jpeg");  //캡처한 이미지 저장
            }
            else if (Cam1_AngleCheck2_cnt == 2) //Check Point1이 눌린 경우
            {
                Bitmap bmp = new Bitmap(Cam1_Check2_openFile.FileName); //Check Point1에서 불러온 이미지 Bitmap 변환
                AngleCheck.Resize(bmp).Save("resize_Cam1_Check2_pb.jpeg");  //이미지 크기 변환, 저장
            }
            Cam1_AngleCheck2_Value_cnt = 1; //Angle Check버튼 활성화

            Mat binary = AngleCheck.Binary(Cam1_Check2_Value, "resize_Cam1_Check2_pb.jpeg");    //저장된 이미지 이진화
            binary = AngleCheck.Pencircle(binary).Item1;    //이진화 이미지에 점찍기
            center[5] = AngleCheck.Pencircle(binary).Item2; center[6] = AngleCheck.Pencircle(binary).Item3; center[7] = AngleCheck.Pencircle(binary).Item4; center[8] = AngleCheck.Pencircle(binary).Item5; //찍은 점의 좌표 저장
            Cam1_Check2_pb.Image = AngleCheck.Penline(new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binary)), center[5], center[6], center[7], center[8]); //두 점으로 선 그리기
            Cam1_Check2_degree.Text = AngleCheck.Degree(center[5], center[6], center[7], center[8]);    //선의 각도
            XY[2] = decimal.ToInt32(Cam1_X.Value); XY[3] = decimal.ToInt32(Cam1_Y.Value);   //이동한 값 저장
            Cam1_cnt2 = 1;
        }

        private void Cam2_AngleCheck1_Click(object sender, EventArgs e) //2.Angle Check1
        {
            if (Cam2_AngleCheck1_cnt == 1)  //Enter가 눌린 경우
            {
                center[10] = center[11] = center[12] = center[13] = 0;  //원의 위치 초기화
                Cam2_Open();
                Bitmap bmp = Save.Image_save(CheckPoint2group.Location.X + Cam2_Camera_pn.Location.X, CheckPoint2group.Location.Y + Cam2_Camera_pn.Location.Y);  //영상 캡처
                bmp.Save("resize_Cam2_Check1_pb.jpeg");  //이미지 크기 변환, 저장
            }
            else if (Cam2_AngleCheck1_cnt == 2) //Check Point1이 눌린 경우
            {
                Bitmap bmp = new Bitmap(Cam2_Check1_openFile.FileName); //Check Point1에서 불러온 이미지 Bitmap 변환
                AngleCheck.Resize(bmp).Save("resize_Cam2_Check1_pb.jpeg");  //이미지 크기 변환, 저장
            }
            Cam2_AngleCheck1_Value_cnt = 1; //Angle Check버튼 활성화

            Mat binary = AngleCheck.Binary(Cam2_Check1_Value, "resize_Cam2_Check1_pb.jpeg");    //저장된 이미지 이진화
            binary = AngleCheck.Pencircle(binary).Item1;    //이진화 이미지에 점찍기
            center[10] = AngleCheck.Pencircle(binary).Item2; center[11] = AngleCheck.Pencircle(binary).Item3; center[12] = AngleCheck.Pencircle(binary).Item4; center[13] = AngleCheck.Pencircle(binary).Item5; //찍은 점의 좌표 저장
            Cam2_Check1_pb.Image = AngleCheck.Penline(new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binary)), center[10], center[11], center[12], center[13]); //두 점으로 선 그리기
            Cam2_Check1_degree.Text = AngleCheck.Degree(center[10], center[11], center[12], center[13]);    //선의 각도
            XY[4] = decimal.ToInt32(Cam2_X.Value); XY[5] = decimal.ToInt32(Cam2_Y.Value);   //이동한 값 저장
            Cam2_cnt1 = 1;
        }

        private void Cam2_AngleCheck2_Click(object sender, EventArgs e) //2.Angle Check2
        {
            if (Cam2_AngleCheck2_cnt == 1)  //Enter가 눌린 경우
            {
                center[15] = center[16] = center[17] = center[18] = 0;  //원의 위치 초기화
                Cam2_Open();
                Bitmap bmp = Save.Image_save(CheckPoint2group.Location.X + Cam2_Camera_pn.Location.X, CheckPoint2group.Location.Y + Cam2_Camera_pn.Location.Y);  //영상 캡처
                bmp.Save("resize_Cam2_Check2_pb.jpeg");  //이미지 크기 변환, 저장
            }
            else if (Cam2_AngleCheck2_cnt == 2) //Check Point2이 눌린 경우
            {
                Bitmap bmp = new Bitmap(Cam2_Check2_openFile.FileName); //Check Point2에서 불러온 이미지 Bitmap 변환
                AngleCheck.Resize(bmp).Save("resize_Cam2_Check2_pb.jpeg");  //이미지 크기 변환, 저장
            }
            Cam2_AngleCheck2_Value_cnt = 1; //Angle Check버튼 활성화

            Mat binary = AngleCheck.Binary(Cam2_Check2_Value, "resize_Cam2_Check2_pb.jpeg");    //저장된 이미지 이진화
            binary = AngleCheck.Pencircle(binary).Item1;    //이진화 이미지에 점찍기
            center[15] = AngleCheck.Pencircle(binary).Item2; center[16] = AngleCheck.Pencircle(binary).Item3; center[17] = AngleCheck.Pencircle(binary).Item4; center[18] = AngleCheck.Pencircle(binary).Item5; //찍은 점의 좌표 저장
            Cam2_Check2_pb.Image = AngleCheck.Penline(new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binary)), center[15], center[16], center[17], center[18]); //두 점으로 선 그리기
            Cam2_Check2_degree.Text = AngleCheck.Degree(center[15], center[16], center[17], center[18]);    //선의 각도
            XY[6] = decimal.ToInt32(Cam2_X.Value); XY[7] = decimal.ToInt32(Cam2_Y.Value);   //이동한 값 저장
            Cam2_cnt2 = 1;
        }

        private void Cam3_AngleCheck1_Click(object sender, EventArgs e) //3.Angle Check1
        {
            if (Cam3_AngleCheck1_cnt == 1)  //Enter가 눌린 경우
            {
                center[20] = center[21] = center[22] = center[23] = 0;  //원의 위치 초기화
                Cam3_Open();
                Bitmap bmp = Save.Image_save(CheckPoint3group.Location.X + Cam3_Camera_pn.Location.X, CheckPoint3group.Location.Y + Cam3_Camera_pn.Location.Y);  //영상 캡처
                bmp.Save("resize_Cam3_Check1_pb.jpeg");  //이미지 크기 변환, 저장
            }
            else if (Cam3_AngleCheck1_cnt == 2) //Check Point1이 눌린 경우
            {
                Bitmap bmp = new Bitmap(Cam3_Check1_openFile.FileName); //Check Point1에서 불러온 이미지 Bitmap 변환
                AngleCheck.Resize(bmp).Save("resize_Cam3_Check1_pb.jpeg");  //이미지 크기 변환, 저장
            }
            Cam3_AngleCheck1_Value_cnt = 1; //Angle Check버튼 활성화

            Mat binary = AngleCheck.Binary(Cam3_Check1_Value, "resize_Cam3_Check1_pb.jpeg");    //저장된 이미지 이진화
            binary = AngleCheck.Pencircle(binary).Item1;    //이진화 이미지에 점찍기
            center[20] = AngleCheck.Pencircle(binary).Item2; center[21] = AngleCheck.Pencircle(binary).Item3; center[22] = AngleCheck.Pencircle(binary).Item4; center[23] = AngleCheck.Pencircle(binary).Item5; //찍은 점의 좌표 저장
            Cam3_Check1_pb.Image = AngleCheck.Penline(new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binary)), center[20], center[21], center[22], center[23]); //두 점으로 선 그리기
            Cam3_Check1_degree.Text = AngleCheck.Degree(center[20], center[21], center[22], center[23]);    //선의 각도
            XY[8] = decimal.ToInt32(Cam3_X.Value); XY[9] = decimal.ToInt32(Cam3_Y.Value);   //이동한 값 저장
            Cam3_cnt1 = 1;
        }

        private void Cam3_AngleCheck2_Click(object sender, EventArgs e) //3.Angle Check2
        {
            if (Cam3_AngleCheck2_cnt == 1)  //Enter가 눌린 경우
            {
                center[25] = center[26] = center[27] = center[28] = 0;  //원의 위치 초기화
                Cam3_Open();
                Bitmap bmp = Save.Image_save(CheckPoint3group.Location.X + Cam3_Camera_pn.Location.X, CheckPoint3group.Location.Y + Cam3_Camera_pn.Location.Y);  //영상 캡처
                bmp.Save("resize_Cam3_Check2_pb.jpeg");  //이미지 크기 변환, 저장
            }
            else if (Cam3_AngleCheck2_cnt == 2) //Check Point2이 눌린 경우
            {
                Bitmap bmp = new Bitmap(Cam3_Check2_openFile.FileName); //Check Point1에서 불러온 이미지 Bitmap 변환
                AngleCheck.Resize(bmp).Save("resize_Cam3_Check2_pb.jpeg");  //이미지 크기 변환, 저장
            }
            Cam3_AngleCheck2_Value_cnt = 1; //Angle Check버튼 활성화
            Mat binary = AngleCheck.Binary(Cam3_Check2_Value, "resize_Cam3_Check2_pb.jpeg");    //저장된 이미지 이진화
            binary = AngleCheck.Pencircle(binary).Item1;    //이진화 이미지에 점찍기
            center[25] = AngleCheck.Pencircle(binary).Item2; center[26] = AngleCheck.Pencircle(binary).Item3; center[27] = AngleCheck.Pencircle(binary).Item4; center[28] = AngleCheck.Pencircle(binary).Item5; //찍은 점의 좌표 저장
            Cam3_Check2_pb.Image = AngleCheck.Penline(new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binary)), center[25], center[26], center[27], center[28]); //두 점으로 선 그리기
            Cam3_Check2_degree.Text = AngleCheck.Degree(center[25], center[26], center[27], center[28]);    //선의 각도
            XY[10] = decimal.ToInt32(Cam3_X.Value); XY[11] = decimal.ToInt32(Cam3_Y.Value);   //이동한 값 저장
            Cam3_cnt2 = 1;
        }

        private void Cam4_AngleCheck1_Click(object sender, EventArgs e) //4.Angle Check1
        {
            if (Cam4_AngleCheck1_cnt == 1)  //Enter가 눌린 경우
            {
                center[30] = center[31] = center[32] = center[33] = 0;  //원의 위치 초기화
                Cam4_Open();
                Bitmap bmp = Save.Image_save(CheckPoint4group.Location.X + Cam4_Camera_pn.Location.X, CheckPoint4group.Location.Y + Cam4_Camera_pn.Location.Y);  //영상 캡처
                bmp.Save("resize_Cam4_Check1_pb.jpeg");  //이미지 크기 변환, 저장
            }
            else if (Cam4_AngleCheck1_cnt == 2) //Check Point1이 눌린 경우
            {
                Bitmap bmp = new Bitmap(Cam4_Check1_openFile.FileName); //Check Point1에서 불러온 이미지 Bitmap 변환
                AngleCheck.Resize(bmp).Save("resize_Cam4_Check1_pb.jpeg");  //이미지 크기 변환, 저장
            }
            Cam4_AngleCheck1_Value_cnt = 1; //Angle Check버튼 활성화

            Mat binary = AngleCheck.Binary(Cam4_Check1_Value, "resize_Cam4_Check1_pb.jpeg");    //저장된 이미지 이진화
            binary = AngleCheck.Pencircle(binary).Item1;    //이진화 이미지에 점찍기
            center[30] = AngleCheck.Pencircle(binary).Item2; center[31] = AngleCheck.Pencircle(binary).Item3; center[32] = AngleCheck.Pencircle(binary).Item4; center[33] = AngleCheck.Pencircle(binary).Item5; //찍은 점의 좌표 저장
            Cam4_Check1_pb.Image = AngleCheck.Penline(new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binary)), center[30], center[31], center[32], center[33]); //두 점으로 선 그리기image;
            Cam4_Check1_degree.Text = AngleCheck.Degree(center[30], center[31], center[32], center[33]);    //선의 각도
            XY[12] = decimal.ToInt32(Cam4_X.Value); XY[13] = decimal.ToInt32(Cam4_Y.Value);   //이동한 값 저장
            Cam4_cnt1 = 1;
        }

        private void Cam4_AngleCheck2_Click(object sender, EventArgs e) //4.Angle Check2
        {
            if (Cam4_AngleCheck2_cnt == 1)  //Enter가 눌린 경우
            {
                center[35] = center[36] = center[37] = center[38] = 0;  //원의 위치 초기화
                Cam4_Open();
                Bitmap bmp = Save.Image_save(CheckPoint4group.Location.X + Cam4_Camera_pn.Location.X, CheckPoint4group.Location.Y + Cam4_Camera_pn.Location.Y);  //영상 캡처
                bmp.Save("resize_Cam4_Check2_pb.jpeg");  //이미지 크기 변환, 저장
            }
            else if (Cam4_AngleCheck2_cnt == 2) //Check Point2이 눌린 경우
            {
                Bitmap bmp = new Bitmap(Cam4_Check2_openFile.FileName); //Check Point1에서 불러온 이미지 Bitmap 변환
                AngleCheck.Resize(bmp).Save("resize_Cam4_Check2_pb.jpeg");  //이미지 크기 변환, 저장
            }
            Cam4_AngleCheck2_Value_cnt = 1; //Angle Check버튼 활성화

            Mat binary = AngleCheck.Binary(Cam4_Check2_Value, "resize_Cam4_Check2_pb.jpeg");    //저장된 이미지 이진화
            binary = AngleCheck.Pencircle(binary).Item1;    //이진화 이미지에 점찍기
            center[35] = AngleCheck.Pencircle(binary).Item2; center[36] = AngleCheck.Pencircle(binary).Item3; center[37] = AngleCheck.Pencircle(binary).Item4; center[38] = AngleCheck.Pencircle(binary).Item5; //찍은 점의 좌표 저장
            Cam4_Check2_pb.Image = AngleCheck.Penline(new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binary)), center[35], center[36], center[37], center[38]); //두 점으로 선 그리기
            Cam4_Check2_degree.Text = AngleCheck.Degree(center[35], center[36], center[37], center[38]);    //선의 각도
            XY[14] = decimal.ToInt32(Cam4_X.Value); XY[15] = decimal.ToInt32(Cam4_Y.Value);   //이동한 값 저장
            Cam4_cnt2 = 1;
        }

        private void Cam5_AngleCheck1_Click(object sender, EventArgs e) //5.Angle Check1
        {
            if (Cam5_AngleCheck1_cnt == 1)  //Enter가 눌린 경우
            {
                center[40] = center[41] = center[42] = center[43] = 0;  //원의 위치 초기화
                Cam5_Open();
                Bitmap bmp = Save.Image_save(CheckPoint5group.Location.X + Cam5_Camera_pn.Location.X, CheckPoint5group.Location.Y + Cam5_Camera_pn.Location.Y);  //영상 캡처
                bmp.Save("resize_Cam5_Check1_pb.jpeg");  //이미지 크기 변환, 저장
            }
            else if (Cam5_AngleCheck1_cnt == 2) //Check Point1이 눌린 경우
            {
                Bitmap bmp = new Bitmap(Cam5_Check1_openFile.FileName); //Check Point1에서 불러온 이미지 Bitmap 변환
                AngleCheck.Resize(bmp).Save("resize_Cam5_Check1_pb.jpeg");  //이미지 크기 변환, 저장
            }
            Cam5_AngleCheck1_Value_cnt = 1; //Angle Check버튼 활성화

            Mat binary = AngleCheck.Binary(Cam5_Check1_Value, "resize_Cam5_Check1_pb.jpeg");    //저장된 이미지 이진화
            binary = AngleCheck.Pencircle(binary).Item1;    //이진화 이미지에 점찍기
            center[40] = AngleCheck.Pencircle(binary).Item2; center[41] = AngleCheck.Pencircle(binary).Item3; center[42] = AngleCheck.Pencircle(binary).Item4; center[43] = AngleCheck.Pencircle(binary).Item5; //찍은 점의 좌표 저장
            Cam5_Check1_pb.Image = AngleCheck.Penline(new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binary)), center[40], center[41], center[42], center[43]); //두 점으로 선 그리기
            Cam5_Check1_degree.Text = AngleCheck.Degree(center[40], center[41], center[42], center[43]);    //선의 각도
            XY[16] = decimal.ToInt32(Cam5_X.Value); XY[17] = decimal.ToInt32(Cam5_Y.Value);   //이동한 값 저장
            Cam5_cnt1 = 1;
        }

        private void Cam5_AngleCheck2_Click(object sender, EventArgs e) //5.Angle Check2
        {
            if (Cam5_AngleCheck2_cnt == 1)  //Enter가 눌린 경우
            {
                center[45] = center[46] = center[47] = center[48] = 0;  //원의 위치 초기화
                Cam5_Open();
                Bitmap bmp = Save.Image_save(CheckPoint5group.Location.X + Cam5_Camera_pn.Location.X, CheckPoint5group.Location.Y + Cam5_Camera_pn.Location.Y);  //영상 캡처
                bmp.Save("resize_Cam5_Check2_pb.jpeg");  //이미지 크기 변환, 저장
            }
            else if (Cam5_AngleCheck2_cnt == 2) //Check Point2이 눌린 경우
            {
                Bitmap bmp = new Bitmap(Cam5_Check2_openFile.FileName); //Check Point1에서 불러온 이미지 Bitmap 변환
                AngleCheck.Resize(bmp).Save("resize_Cam5_Check2_pb.jpeg");  //이미지 크기 변환, 저장
            }
            Cam5_AngleCheck2_Value_cnt = 1; //Angle Check버튼 활성화

            Mat binary = AngleCheck.Binary(Cam5_Check2_Value, "resize_Cam5_Check2_pb.jpeg");    //저장된 이미지 이진화
            binary = AngleCheck.Pencircle(binary).Item1;    //이진화 이미지에 점찍기
            center[45] = AngleCheck.Pencircle(binary).Item2; center[46] = AngleCheck.Pencircle(binary).Item3; center[47] = AngleCheck.Pencircle(binary).Item4; center[48] = AngleCheck.Pencircle(binary).Item5; //찍은 점의 좌표 저장
            Cam5_Check2_pb.Image = AngleCheck.Penline(new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binary)), center[45], center[46], center[47], center[48]); //두 점으로 선 그리기;
            Cam5_Check2_degree.Text = AngleCheck.Degree(center[45], center[46], center[47], center[48]);    //선의 각도
            XY[18] = decimal.ToInt32(Cam5_X.Value); XY[19] = decimal.ToInt32(Cam5_Y.Value);   //이동한 값 저장
            Cam5_cnt2 = 1;
        }

        private void Cam1_Check1_txt_TextChanged(object sender, EventArgs e)   //1.Check1
        {
            if (TXT.TXT_to_INT(Cam1_Check1_txt.Text) >= 256) { Cam1_Check1_txt.Text = "0"; }   //입력값이 256보다 클 경우 0으로 입력
            Cam1_Check1_Value = TXT.TXT_to_INT(Cam1_Check1_txt.Text); //입력값을 Value값으로 저장
            if (Cam1_AngleCheck1_Value_cnt == 1) { Cam1_AngleCheck1_Click(sender, e); } //cnt가 1일 경우 AngleCheck1 누르기
            Cam1_Check1_Bar.Value = (int)Cam1_Check1_Value; //Check1_Bar의 값과 Check1_txt의 값 일치시키기
        }

        private void Cam1_Check2_txt_TextChanged(object sender, EventArgs e)    //1.Check2
        {
            if (TXT.TXT_to_INT(Cam1_Check2_txt.Text) >= 256) { Cam1_Check2_txt.Text = "0"; }   //입력값이 256보다 클 경우 0으로 입력
            Cam1_Check2_Value = TXT.TXT_to_INT(Cam1_Check2_txt.Text); //입력값을 Value값으로 저장
            if (Cam1_AngleCheck2_Value_cnt == 1) { Cam1_AngleCheck2_Click(sender, e); } //cnt가 1일 경우 AngleCheck2 누르기
            Cam1_Check2_Bar.Value = (int)Cam1_Check2_Value; //Check2_Bar의 값과 Check2_txt의 값 일치시키기
        }

        private void Cam2_Check1_txt_TextChanged(object sender, EventArgs e)    //2.Check1
        {
            if (TXT.TXT_to_INT(Cam2_Check1_txt.Text) >= 256) { Cam2_Check1_txt.Text = "0"; }   //입력값이 256보다 클 경우 0으로 입력
            Cam2_Check1_Value = TXT.TXT_to_INT(Cam2_Check1_txt.Text); //입력값을 Value값으로 저장
            if (Cam2_AngleCheck1_Value_cnt == 1) { Cam2_AngleCheck1_Click(sender, e); } //cnt가 1일 경우 AngleCheck1 누르기
            Cam2_Check1_Bar.Value = (int)Cam2_Check1_Value; ; //Check1_Bar의 값과 Check1_txt의 값 일치시키기
        }

        private void Cam2_Check2_txt_TextChanged(object sender, EventArgs e)    //2.Check2
        {
            if (TXT.TXT_to_INT(Cam2_Check2_txt.Text) >= 256) { Cam2_Check2_txt.Text = "0"; }   //입력값이 256보다 클 경우 0으로 입력
            Cam2_Check2_Value = TXT.TXT_to_INT(Cam2_Check2_txt.Text); //입력값을 Value값으로 저장
            if (Cam2_AngleCheck2_Value_cnt == 1) { Cam2_AngleCheck2_Click(sender, e); } //cnt가 1일 경우 AngleCheck2 누르기
            Cam2_Check2_Bar.Value = (int)Cam2_Check2_Value; //Check2_Bar의 값과 Check2_txt의 값 일치시키기
        }

        private void Cam3_Check1_txt_TextChanged(object sender, EventArgs e)    //3.Check1
        {
            if (TXT.TXT_to_INT(Cam3_Check1_txt.Text) >= 256) { Cam3_Check1_txt.Text = "0"; }   //입력값이 256보다 클 경우 0으로 입력
            Cam3_Check1_Value = TXT.TXT_to_INT(Cam3_Check1_txt.Text); //입력값을 Value값으로 저장
            if (Cam3_AngleCheck1_Value_cnt == 1) { Cam3_AngleCheck1_Click(sender, e); } //cnt가 1일 경우 AngleCheck1 누르기
            Cam3_Check1_Bar.Value = (int)Cam3_Check1_Value; ; //Check1_Bar의 값과 Check1_txt의 값 일치시키기
        }

        private void Cam3_Check2_txt_TextChanged(object sender, EventArgs e)    //3.Check2
        {
            if (TXT.TXT_to_INT(Cam3_Check2_txt.Text) >= 256) { Cam3_Check2_txt.Text = "0"; }   //입력값이 256보다 클 경우 0으로 입력
            Cam3_Check2_Value = TXT.TXT_to_INT(Cam3_Check2_txt.Text); //입력값을 Value값으로 저장
            if (Cam3_AngleCheck2_Value_cnt == 1) { Cam3_AngleCheck2_Click(sender, e); } //cnt가 1일 경우 AngleCheck2 누르기
            Cam3_Check2_Bar.Value = (int)Cam3_Check2_Value; //Check2_Bar의 값과 Check2_txt의 값 일치시키기
        }

        private void Cam4_Check1_txt_TextChanged(object sender, EventArgs e)    //4.Check1
        {
            if (TXT.TXT_to_INT(Cam4_Check1_txt.Text) >= 256) { Cam4_Check1_txt.Text = "0"; }   //입력값이 256보다 클 경우 0으로 입력
            Cam4_Check1_Value = TXT.TXT_to_INT(Cam4_Check1_txt.Text); //입력값을 Value값으로 저장
            if (Cam4_AngleCheck1_Value_cnt == 1) { Cam4_AngleCheck1_Click(sender, e); } //cnt가 1일 경우 AngleCheck1 누르기
            Cam4_Check1_Bar.Value = (int)Cam4_Check1_Value; //Check1_Bar의 값과 Check1_txt의 값 일치시키기
        }

        private void Cam4_Check2_txt_TextChanged(object sender, EventArgs e)    //4.Check2
        {
            if (TXT.TXT_to_INT(Cam4_Check2_txt.Text) >= 256) { Cam4_Check2_txt.Text = "0"; }   //입력값이 256보다 클 경우 0으로 입력
            Cam4_Check2_Value = TXT.TXT_to_INT(Cam4_Check2_txt.Text); //입력값을 Value값으로 저장
            if (Cam4_AngleCheck2_Value_cnt == 1) { Cam4_AngleCheck2_Click(sender, e); } //cnt가 1일 경우 AngleCheck2 누르기
            Cam4_Check2_Bar.Value = (int)Cam4_Check2_Value; //Check2_Bar의 값과 Check2_txt의 값 일치시키기
        }

        private void Cam5_Check1_txt_TextChanged(object sender, EventArgs e)    //5.Check1
        {
            if (TXT.TXT_to_INT(Cam5_Check1_txt.Text) >= 256) { Cam5_Check1_txt.Text = "0"; }   //입력값이 256보다 클 경우 0으로 입력
            Cam5_Check1_Value = TXT.TXT_to_INT(Cam5_Check1_txt.Text); //입력값을 Value값으로 저장
            if (Cam5_AngleCheck1_Value_cnt == 1) { Cam5_AngleCheck1_Click(sender, e); } //cnt가 1일 경우 AngleCheck1 누르기
            Cam5_Check1_Bar.Value = (int)Cam5_Check1_Value; //Check1_Bar의 값과 Check1_txt의 값 일치시키기
        }

        private void Cam5_Check2_txt_TextChanged(object sender, EventArgs e)    //5.Check2
        {
            if (TXT.TXT_to_INT(Cam5_Check2_txt.Text) >= 256) { Cam5_Check2_txt.Text = "0"; }   //입력값이 256보다 클 경우 0으로 입력
            Cam5_Check2_Value = TXT.TXT_to_INT(Cam5_Check2_txt.Text); //입력값을 Value값으로 저장
            if (Cam5_AngleCheck2_Value_cnt == 1) { Cam5_AngleCheck2_Click(sender, e); } //cnt가 1일 경우 AngleCheck2 누르기
            Cam5_Check2_Bar.Value = (int)Cam5_Check2_Value; //Check2_Bar의 값과 Check2_txt의 값 일치시키기
        }

        private void Cam1_Anglediff_TextChanged(object sender, EventArgs e) //1.각도차
        {
            if ((Cam1_Check1_pb.Image != null) && (Cam1_Check2_pb.Image != null)) { Cam1_Anglediff.Text = AngleCheck.Angle(Convert.ToDouble(Cam1_Check1_degree.Text), Convert.ToDouble(Cam1_Check2_degree.Text)); }    //Check1, Check2에 이미지가 있을 경우에 각도 차 계산
            else { Cam1_Anglediff.Text = "0.00"; }  //Check1, Check2에 이미지가 없을 경우 "0.00"으로 입력
        }

        private void Cam2_Anglediff_TextChanged(object sender, EventArgs e) //2.각도차
        {
            if ((Cam2_Check1_pb.Image != null) && (Cam2_Check2_pb.Image != null)) { Cam2_Anglediff.Text = AngleCheck.Angle(Convert.ToDouble(Cam2_Check1_degree.Text), Convert.ToDouble(Cam2_Check2_degree.Text)); }    //Check1, Check2에 이미지가 있을 경우에 각도 차 계산
            else { Cam2_Anglediff.Text = "0.00"; }  //Check1, Check2에 이미지가 없을 경우 "0.00"으로 입력
        }

        private void Cam3_Anglediff_TextChanged(object sender, EventArgs e) //3.각도차
        {
            if ((Cam3_Check1_pb.Image != null) && (Cam3_Check2_pb.Image != null)) { Cam3_Anglediff.Text = AngleCheck.Angle(Convert.ToDouble(Cam3_Check1_degree.Text), Convert.ToDouble(Cam3_Check2_degree.Text)); }    //Check1, Check2에 이미지가 있을 경우에 각도 차 계산
            else { Cam3_Anglediff.Text = "0.00"; }  //Check1, Check2에 이미지가 없을 경우 "0.00"으로 입력
        }

        private void Cam4_Anglediff_TextChanged(object sender, EventArgs e) //4.각도차
        {
            if ((Cam4_Check1_pb.Image != null) && (Cam4_Check2_pb.Image != null)) { Cam4_Anglediff.Text = AngleCheck.Angle(Convert.ToDouble(Cam4_Check1_degree.Text), Convert.ToDouble(Cam4_Check2_degree.Text)); }    //Check1, Check2에 이미지가 있을 경우에 각도 차 계산
            else { Cam4_Anglediff.Text = "0.00"; }  //Check1, Check2에 이미지가 없을 경우 "0.00"으로 입력
        }

        private void Cam5_Anglediff_TextChanged(object sender, EventArgs e) //5.각도차
        {
            if ((Cam5_Check1_pb.Image != null) && (Cam5_Check2_pb.Image != null)) { Cam5_Anglediff.Text = AngleCheck.Angle(Convert.ToDouble(Cam5_Check1_degree.Text), Convert.ToDouble(Cam5_Check2_degree.Text)); }    //Check1, Check2에 이미지가 있을 경우에 각도 차 계산
            else { Cam5_Anglediff.Text = "0.00"; }  //Check1, Check2에 이미지가 없을 경우 "0.00"으로 입력
        }

        private void Cam1_Open()    //Cam1 열기
        {
            cam1.Read(frame1);  //Cam1에 들어오는 영상 가져오기
            Cam1_X.Enabled = true; Cam1_Y.Enabled = true; Cam1_mag.Enabled = true;  //영상이 켜지면 좌표 선택, 배율 선택 활성화
            Bitmap pic1 = AngleCheck.Resize(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame1));   //영상 크기 Picture에 맞게 바꾸기
            pic1 = Position.Mag_Image(pic1, (int)(pic1.Width * Convert.ToDouble(Cam1_mag.Text) / 100), (int)(pic1.Height * Convert.ToDouble(Cam1_mag.Text) / 100)); //배율 입력시 이미지 확대,축소
            Cam1_X.Minimum = -pic1.Width;   Cam1_Y.Minimum= -pic1.Height;
            if ((Cam1_AngleCheck1_Value_cnt == 1) && (Cam1_AngleCheck2_Value_cnt != 1))    //AngleCheck1이 눌린 경우 영상에 선그리기
            {
                if (Cam1_cnt1 == 1) { pic1.Save("resize_Cam1_Check1_pb.jpeg"); Cam1_cnt1 = 0; }  //캡처한 이미지 저장
                Cam1_Camera_pb.Image = AngleCheck.Penline(pic1, (center[0] - TXT.TXT_to_INT(Cam1_X.Text)), (center[1] - TXT.TXT_to_INT(Cam1_Y.Text)), (center[2] - TXT.TXT_to_INT(Cam1_X.Text)), (center[3] - TXT.TXT_to_INT(Cam1_Y.Text)));    //영상에 선 그리기 

            }
            else if ((Cam1_AngleCheck1_Value_cnt != 1) && (Cam1_AngleCheck2_Value_cnt == 1)) //AngleCheck2이 눌린 경우 영상에 선그리기
            {
                if (Cam1_cnt2 == 1) { pic1.Save("resize_Cam1_Check2_pb.jpeg"); Cam1_cnt2 = 0; }  //캡처한 이미지 저장
                Cam1_Camera_pb.Image = AngleCheck.Penline(pic1, (center[5] - TXT.TXT_to_INT(Cam1_X.Text)), (center[6] - TXT.TXT_to_INT(Cam1_Y.Text)), (center[7] - TXT.TXT_to_INT(Cam1_X.Text)), (center[8] - TXT.TXT_to_INT(Cam1_Y.Text)));    //영상에 선 그리기
            }
            else if ((Cam1_AngleCheck1_Value_cnt == 1) && (Cam1_AngleCheck2_Value_cnt == 1)) //AngleCheck1과 2가 눌린 경우 영상에 선그리기
            {
                if (Cam1_cnt1 == 1) { pic1.Save("resize_Cam1_Check1_pb.jpeg"); Cam1_cnt1 = 0; }  //캡처한 이미지 저장
                if (Cam1_cnt2 == 1) { pic1.Save("resize_Cam1_Check2_pb.jpeg"); Cam1_cnt2 = 0; }  //캡처한 이미지 저장
                pic1 = AngleCheck.Penline(pic1, (center[0] - TXT.TXT_to_INT(Cam1_X.Text)), (center[1] - TXT.TXT_to_INT(Cam1_Y.Text)), (center[2] - TXT.TXT_to_INT(Cam1_X.Text)), (center[3] - TXT.TXT_to_INT(Cam1_Y.Text)));    //AngleCheck1의 선 그리기
                Cam1_Camera_pb.Image = AngleCheck.Penline(pic1, (center[5] - TXT.TXT_to_INT(Cam1_X.Text)), (center[6] - TXT.TXT_to_INT(Cam1_Y.Text)), (center[7] - TXT.TXT_to_INT(Cam1_X.Text)), (center[8] - TXT.TXT_to_INT(Cam1_Y.Text))); ;    //영상에 선 그리기
            }
            else { Cam1_Camera_pb.Image = pic1; }
        }

        private void Cam2_Open()    //Cam2 열기
        {
            cam2.Read(frame2);  //Cam2에 들어오는 영상 가져오기
            Cam2_X.Enabled = true; Cam2_Y.Enabled = true; Cam2_mag.Enabled = true;  //영상이 켜지면 좌표 선택, 배율 선택 활성화
            Bitmap pic2 = AngleCheck.Resize(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame2));   //영상 크기 Picture에 맞게 바꾸기
            pic2 = (Bitmap)Position.Mag_Image(pic2, (int)(pic2.Width * Convert.ToDouble(Cam2_mag.Text) / 100), (int)(pic2.Height * Convert.ToDouble(Cam2_mag.Text) / 100)); //배율 입력시 이미지 확대,축소
            Cam2_X.Minimum = -pic2.Width; Cam2_Y.Minimum = -pic2.Height;
            if ((Cam2_AngleCheck1_Value_cnt == 1) && (Cam2_AngleCheck2_Value_cnt != 1))    //AngleCheck1이 눌린 경우 영상에 선그리기
            {
                if (Cam2_cnt1 == 1) { pic2.Save("resize_Cam2_Check1_pb.jpeg"); Cam2_cnt1 = 0; }  //캡처한 이미지 저장
                Cam2_Camera_pb.Image = AngleCheck.Penline(pic2, center[10] - TXT.TXT_to_INT(Cam2_X.Text), center[11] - TXT.TXT_to_INT(Cam2_Y.Text), center[12] - TXT.TXT_to_INT(Cam2_X.Text), center[13] - TXT.TXT_to_INT(Cam2_Y.Text));    //영상에 선 그리기
            }
            else if ((Cam2_AngleCheck1_Value_cnt != 1) && (Cam2_AngleCheck2_Value_cnt == 1)) //AngleCheck2이 눌린 경우 영상에 선그리기
            {
                if (Cam2_cnt2 == 1) { pic2.Save("resize_Cam2_Check2_pb.jpeg"); Cam2_cnt2 = 0; }  //캡처한 이미지 저장
                Cam2_Camera_pb.Image = AngleCheck.Penline(pic2, center[15] - TXT.TXT_to_INT(Cam2_X.Text), center[16] - TXT.TXT_to_INT(Cam2_Y.Text), center[17] - TXT.TXT_to_INT(Cam2_X.Text), center[18] - TXT.TXT_to_INT(Cam2_Y.Text));    //영상에 선 그리기
            }
            else if ((Cam2_AngleCheck1_Value_cnt == 1) && (Cam2_AngleCheck2_Value_cnt == 1)) //AngleCheck1과 2가 눌린 경우 영상에 선그리기
            {
                if (Cam2_cnt1 == 1) { pic2.Save("resize_Cam2_Check1_pb.jpeg"); Cam2_cnt1 = 0; }  //캡처한 이미지 저장
                if (Cam2_cnt2 == 1) { pic2.Save("resize_Cam2_Check2_pb.jpeg"); Cam2_cnt2 = 0; }  //캡처한 이미지 저장
                pic2 = AngleCheck.Penline(pic2, center[10] - TXT.TXT_to_INT(Cam2_X.Text), center[11] - TXT.TXT_to_INT(Cam2_Y.Text), center[12] - TXT.TXT_to_INT(Cam2_X.Text), center[13] - TXT.TXT_to_INT(Cam2_Y.Text));    //AngleCheck1의 선 그리기
                Cam2_Camera_pb.Image = AngleCheck.Penline(pic2, center[15] - TXT.TXT_to_INT(Cam2_X.Text), center[16] - TXT.TXT_to_INT(Cam2_Y.Text), center[17] - TXT.TXT_to_INT(Cam2_X.Text), center[18] - TXT.TXT_to_INT(Cam2_Y.Text));    //영상에 선 그리기
            }
            else { Cam2_Camera_pb.Image = pic2; } //영상 띄우기
        }

        private void Cam3_Open()    //Cam3 열기
        {
            cam3.Read(frame3);  //Cam3에 들어오는 영상 가져오기
            Cam3_X.Enabled = true; Cam3_Y.Enabled = true; Cam3_mag.Enabled = true;  //영상이 켜지면 좌표 선택, 배율 선택 활성화
            Bitmap pic3 = AngleCheck.Resize(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame3));   //영상 크기 Picture에 맞게 바꾸기
            pic3 = (Bitmap)Position.Mag_Image(pic3, (int)(pic3.Width * Convert.ToDouble(Cam3_mag.Text) / 100), (int)(pic3.Height * Convert.ToDouble(Cam3_mag.Text) / 100)); //배율 입력시 이미지 확대,축소
            Cam3_X.Minimum = -pic3.Width; Cam3_Y.Minimum = -pic3.Height;
            if ((Cam3_AngleCheck1_Value_cnt == 1) && (Cam3_AngleCheck2_Value_cnt != 1))    //AngleCheck1이 눌린 경우 영상에 선그리기
            {
                if (Cam3_cnt1 == 1) { pic3.Save("resize_Cam3_Check1_pb.jpeg"); Cam3_cnt1 = 0; }  //캡처한 이미지 저장
                Cam3_Camera_pb.Image = AngleCheck.Penline(pic3, center[20] - TXT.TXT_to_INT(Cam3_X.Text), center[21] - TXT.TXT_to_INT(Cam3_Y.Text), center[22] - TXT.TXT_to_INT(Cam3_X.Text), center[23] - TXT.TXT_to_INT(Cam3_Y.Text));    //영상에 선 그리기
            }
            else if ((Cam3_AngleCheck1_Value_cnt != 1) && (Cam3_AngleCheck2_Value_cnt == 1)) //AngleCheck2이 눌린 경우 영상에 선그리기
            {
                if (Cam3_cnt2 == 1) { pic3.Save("resize_Cam3_Check2_pb.jpeg"); Cam3_cnt2 = 0; }  //캡처한 이미지 저장
                Cam3_Camera_pb.Image = AngleCheck.Penline(pic3, center[25] - TXT.TXT_to_INT(Cam3_X.Text), center[26] - TXT.TXT_to_INT(Cam3_Y.Text), center[27] - TXT.TXT_to_INT(Cam3_X.Text), center[28] - TXT.TXT_to_INT(Cam3_Y.Text));    //영상에 선 그리기
            }
            else if ((Cam3_AngleCheck1_Value_cnt == 1) && (Cam3_AngleCheck2_Value_cnt == 1)) //AngleCheck1과 2가 눌린 경우 영상에 선그리기
            {
                if (Cam3_cnt1 == 1) { pic3.Save("resize_Cam3_Check1_pb.jpeg"); Cam3_cnt1 = 0; }  //캡처한 이미지 저장
                if (Cam3_cnt2 == 1) { pic3.Save("resize_Cam3_Check2_pb.jpeg"); Cam3_cnt2 = 0; }  //캡처한 이미지 저장
                pic3 = AngleCheck.Penline(pic3, center[20] - TXT.TXT_to_INT(Cam3_X.Text), center[21] - TXT.TXT_to_INT(Cam3_Y.Text), center[22] - TXT.TXT_to_INT(Cam3_X.Text), center[23] - TXT.TXT_to_INT(Cam3_Y.Text));    //AngleCheck1의 선 그리기
                Cam3_Camera_pb.Image = AngleCheck.Penline(pic3, center[25] - TXT.TXT_to_INT(Cam3_X.Text), center[26] - TXT.TXT_to_INT(Cam3_Y.Text), center[27] - TXT.TXT_to_INT(Cam3_X.Text), center[28] - TXT.TXT_to_INT(Cam3_Y.Text));    //영상에 선 그리기
            }
            else { Cam3_Camera_pb.Image = pic3; } //영상 띄우기
        }

        private void Cam4_Open()    //Cam4 열기
        {
            cam4.Read(frame4);  //Cam4에 들어오는 영상 가져오기
            Cam4_X.Enabled = true; Cam4_Y.Enabled = true; Cam4_mag.Enabled = true;  //영상이 켜지면 좌표 선택, 배율 선택 활성화
            Bitmap pic4 = AngleCheck.Resize(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame4));   //영상 크기 Picture에 맞게 바꾸기
            pic4 = (Bitmap)Position.Mag_Image(pic4, (int)(pic4.Width * Convert.ToDouble(Cam4_mag.Text) / 100), (int)(pic4.Height * Convert.ToDouble(Cam4_mag.Text) / 100)); //배율 입력시 이미지 확대,축소
            Cam4_X.Minimum = -pic4.Width; Cam4_Y.Minimum = -pic4.Height;
            if ((Cam4_AngleCheck1_Value_cnt == 1) && (Cam4_AngleCheck2_Value_cnt != 1))    //AngleCheck1이 눌린 경우 영상에 선그리기
            {
                if (Cam4_cnt1 == 1) { pic4.Save("resize_Cam4_Check1_pb.jpeg"); Cam4_cnt1 = 0; }  //캡처한 이미지 저장
                Cam4_Camera_pb.Image = AngleCheck.Penline(pic4, center[30] - TXT.TXT_to_INT(Cam4_X.Text), center[31] - TXT.TXT_to_INT(Cam4_Y.Text), center[32] - TXT.TXT_to_INT(Cam4_X.Text), center[33] - TXT.TXT_to_INT(Cam4_Y.Text));    //영상에 선 그리기
            }
            else if ((Cam4_AngleCheck1_Value_cnt != 1) && (Cam4_AngleCheck2_Value_cnt == 1)) //AngleCheck2이 눌린 경우 영상에 선그리기
            {
                if (Cam4_cnt2 == 1) { pic4.Save("resize_Cam4_Check2_pb.jpeg"); Cam4_cnt2 = 0; }  //캡처한 이미지 저장
                Cam4_Camera_pb.Image = AngleCheck.Penline(pic4, center[35 - TXT.TXT_to_INT(Cam4_X.Text)], center[36] - TXT.TXT_to_INT(Cam4_Y.Text), center[37] - TXT.TXT_to_INT(Cam4_X.Text), center[38] - TXT.TXT_to_INT(Cam4_Y.Text));    //영상에 선 그리기
            }
            else if ((Cam4_AngleCheck1_Value_cnt == 1) && (Cam4_AngleCheck2_Value_cnt == 1)) //AngleCheck1과 2가 눌린 경우 영상에 선그리기
            {
                if (Cam4_cnt1 == 1) { pic4.Save("resize_Cam4_Check1_pb.jpeg"); Cam4_cnt1 = 0; }  //캡처한 이미지 저장
                if (Cam4_cnt2 == 1) { pic4.Save("resize_Cam4_Check2_pb.jpeg"); Cam4_cnt2 = 0; }  //캡처한 이미지 저장
                pic4 = AngleCheck.Penline(pic4, center[30] - TXT.TXT_to_INT(Cam4_X.Text), center[31] - TXT.TXT_to_INT(Cam4_Y.Text), center[32] - TXT.TXT_to_INT(Cam4_X.Text), center[33] - TXT.TXT_to_INT(Cam4_Y.Text));    //AngleCheck1의 선 그리기
                Cam4_Camera_pb.Image = AngleCheck.Penline(pic4, center[35] - TXT.TXT_to_INT(Cam4_X.Text), center[36] - TXT.TXT_to_INT(Cam4_Y.Text), center[37] - TXT.TXT_to_INT(Cam4_X.Text), center[38] - TXT.TXT_to_INT(Cam4_Y.Text));    //영상에 선 그리기
            }
            else { Cam4_Camera_pb.Image = pic4; } //영상 띄우기
        }

        private void Cam5_Open()    //Cam5 열기
        {
            cam5.Read(frame5);  //Cam5에 들어오는 영상 가져오기
            Cam5_X.Enabled = true; Cam5_Y.Enabled = true; Cam5_mag.Enabled = true;  //영상이 켜지면 좌표 선택, 배율 선택 활성화
            Bitmap pic5 = AngleCheck.Resize(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame5));   //영상 크기 Picture에 맞게 바꾸기
            pic5 = (Bitmap)Position.Mag_Image(pic5, (int)(pic5.Width * Convert.ToDouble(Cam5_mag.Text) / 100), (int)(pic5.Height * Convert.ToDouble(Cam5_mag.Text) / 100)); //배율 입력시 이미지 확대,축소
            Cam5_X.Minimum = -pic5.Width; Cam5_Y.Minimum = -pic5.Height;
            if ((Cam5_AngleCheck1_Value_cnt == 1) && (Cam5_AngleCheck2_Value_cnt != 1))     //AngleCheck1이 눌린 경우 영상에 선그리기
            {
                if (Cam5_cnt1 == 1) { pic5.Save("resize_Cam5_Check1_pb.jpeg"); Cam5_cnt1 = 0; }  //캡처한 이미지 저장
                Cam5_Camera_pb.Image = AngleCheck.Penline(pic5, center[40] - TXT.TXT_to_INT(Cam5_X.Text), center[41] - TXT.TXT_to_INT(Cam5_Y.Text), center[42] - TXT.TXT_to_INT(Cam5_X.Text), center[43] - TXT.TXT_to_INT(Cam5_Y.Text));    //영상에 선 그리기
            }
            else if((Cam5_AngleCheck1_Value_cnt != 1) && (Cam5_AngleCheck2_Value_cnt == 1)) //AngleCheck2이 눌린 경우 영상에 선그리기
            {
                if (Cam5_cnt2 == 1) { pic5.Save("resize_Cam5_Check2_pb.jpeg"); Cam5_cnt2 = 0; }  //캡처한 이미지 저장
                Cam5_Camera_pb.Image = AngleCheck.Penline(pic5, center[45] - TXT.TXT_to_INT(Cam5_X.Text), center[46] - TXT.TXT_to_INT(Cam5_Y.Text), center[47] - TXT.TXT_to_INT(Cam5_X.Text), center[48] - TXT.TXT_to_INT(Cam5_Y.Text));    //영상에 선 그리기
            }
            else if((Cam5_AngleCheck1_Value_cnt == 1) && (Cam5_AngleCheck2_Value_cnt == 1)) //AngleCheck1과 2가 눌린 경우 영상에 선그리기
            {
                if (Cam5_cnt1 == 1) { pic5.Save("resize_Cam5_Check1_pb.jpeg"); Cam5_cnt1 = 0; }  //캡처한 이미지 저장
                if (Cam5_cnt2 == 1) { pic5.Save("resize_Cam5_Check2_pb.jpeg"); Cam5_cnt2 = 0; }  //캡처한 이미지 저장
                pic5 = AngleCheck.Penline(pic5, center[40] - TXT.TXT_to_INT(Cam5_X.Text), center[41] - TXT.TXT_to_INT(Cam5_Y.Text), center[42] - TXT.TXT_to_INT(Cam5_X.Text), center[43] - TXT.TXT_to_INT(Cam5_Y.Text));    //AngleCheck1의 선 그리기
                Cam5_Camera_pb.Image = AngleCheck.Penline(pic5, center[45] - TXT.TXT_to_INT(Cam5_X.Text), center[46] - TXT.TXT_to_INT(Cam5_Y.Text), center[47] - TXT.TXT_to_INT(Cam5_X.Text), center[48] - TXT.TXT_to_INT(Cam5_Y.Text));    //영상에 선 그리기
            }
            else { Cam5_Camera_pb.Image = pic5; } //영상 띄우기
        }

        private void Btn_Enter_Click(object sender, EventArgs e)    //Camera
        {   //모든 AngleCheck_cnt 값 1로 설정(카메라가 켜졌는지 확인)
            Cam1_AngleCheck1_cnt = 1; Cam1_AngleCheck2_cnt = 1; Cam2_AngleCheck1_cnt = 1; Cam2_AngleCheck2_cnt = 1; Cam3_AngleCheck1_cnt = 1; Cam3_AngleCheck2_cnt = 1; Cam4_AngleCheck1_cnt = 1; Cam4_AngleCheck2_cnt = 1; Cam5_AngleCheck1_cnt = 1; Cam5_AngleCheck2_cnt = 1;
            cam1 = new VideoCapture(index1);    frame1 = new Mat(); //index 값으로 카메라 설정, cam의 영상을 저장할 공간
            cam2 = new VideoCapture(index2);    frame2 = new Mat();
            cam3 = new VideoCapture(index3);    frame3 = new Mat();
            cam4 = new VideoCapture(index4);    frame4 = new Mat();
            cam5 = new VideoCapture(index5);    frame5 = new Mat();
            if ((index1 == -1) && (index2 == -1) && (index3 == -1) && (index4 == -1) && (index5 == -1))         //cam이 전부 꺼져 있을 경우    //00000(cam1~cam5)
            {
                Cam1_Camera_pb.Image = null;    //모든 영상 종료
                Cam2_Camera_pb.Image = null;
                Cam3_Camera_pb.Image = null;
                Cam4_Camera_pb.Image = null;
                Cam5_Camera_pb.Image = null;
                MessageBox.Show("카메라를 설정해주세요");
            }
            else if ((index1 != -1) && (index2 == -1) && (index3 == -1) && (index4 == -1) && (index5 == -1))    //cam1만 켜져 있을 경우        //10000
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if (frame1 == null) { break; }
                    if(index1 == -1) { break; }
                    Cam1_Open();
                    Cam2_Camera_pb.Image = null;
                    Cam3_Camera_pb.Image = null;
                    Cam4_Camera_pb.Image = null;
                    Cam5_Camera_pb.Image = null;
                }
            }
            else if ((index1 == -1) && (index2 != -1) && (index3 == -1) && (index4 == -1) && (index5 == -1))    //cam2만 켜져있을 경우         //01000
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if (frame2 == null) { break; }
                    if (index2 == -1) { break; }
                    Cam1_Camera_pb.Image = null;
                    Cam2_Open();
                    Cam3_Camera_pb.Image = null;
                    Cam4_Camera_pb.Image = null;
                    Cam5_Camera_pb.Image = null;
                }
            }
            else if ((index1 == -1) && (index2 == -1) && (index3 != -1) && (index4 == -1) && (index5 == -1))    //cam3만 켜져있을 경우         //00100
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if (frame3 == null) { break; }
                    if (index3 == -1) { break; }
                    Cam1_Camera_pb.Image = null;
                    Cam2_Camera_pb.Image = null;
                    Cam3_Open();
                    Cam4_Camera_pb.Image = null;
                    Cam5_Camera_pb.Image = null;
                }
            }
            else if ((index1 == -1) && (index2 == -1) && (index3 == -1) && (index4 != -1) && (index5 == -1))    //cam4만 켜져있을 경우         //00010
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if (frame4 == null) { break; }
                    if (index4 == -1) { break; }
                    Cam1_Camera_pb.Image = null;
                    Cam2_Camera_pb.Image = null;
                    Cam3_Camera_pb.Image = null;
                    Cam4_Open();
                    Cam5_Camera_pb.Image = null;
                }
            }
            else if ((index1 == -1) && (index2 == -1) && (index3 == -1) && (index4 == -1) && (index5 != -1))    //cam5만 켜져있을 경우         //00001
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if (frame5 == null) { break; }
                    if (index5 == -1) { break; }
                    Cam1_Camera_pb.Image = null;
                    Cam2_Camera_pb.Image = null;
                    Cam3_Camera_pb.Image = null;
                    Cam4_Camera_pb.Image = null;
                    Cam5_Open();
                }
            }
            else if ((index1 != -1) && (index2 != -1) && (index3 == -1) && (index4 == -1) && (index5 == -1))    //cam1, 2만 켜져있을 경우      //11000
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame1 == null) || (frame2 == null)) { break; }
                    if ((index1 == -1) || (index2 == -1)) { break; }
                    Cam1_Open();
                    Cam2_Open();
                    Cam3_Camera_pb.Image = null;
                    Cam4_Camera_pb.Image = null;
                    Cam5_Camera_pb.Image = null;
                }
            }
            else if ((index1 != -1) && (index2 == -1) && (index3 != -1) && (index4 == -1) && (index5 == -1))    //cam1, 3만 켜져있을 경우      //10100
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame1 == null) || (frame3 == null)) { break; }
                    if ((index1 == -1) || (index3 == -1)) { break; }
                    Cam1_Open();
                    Cam2_Camera_pb.Image = null;
                    Cam3_Open();
                    Cam4_Camera_pb.Image = null;
                    Cam5_Camera_pb.Image = null;
                }
            }
            else if ((index1 != -1) && (index2 == -1) && (index3 == -1) && (index4 != -1) && (index5 == -1))    //cam1, 4만 켜져있을 경우      //10010
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame1 == null) || (frame4 == null)) { break; }
                    if ((index1 == -1) || (index4 == -1)) { break; }
                    Cam1_Open();
                    Cam2_Camera_pb.Image = null;
                    Cam3_Camera_pb.Image = null;
                    Cam4_Open();
                    Cam5_Camera_pb.Image = null;
                }
            }
            else if ((index1 != -1) && (index2 == -1) && (index3 == -1) && (index4 == -1) && (index5 != -1))    //cam1, 5만 켜져있을 경우      //10001
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame1 == null) || (frame5 == null)) { break; }
                    if ((index1 == -1) || (index5 == -1)) { break; }
                    Cam1_Open();
                    Cam2_Camera_pb.Image = null;
                    Cam3_Camera_pb.Image = null;
                    Cam4_Camera_pb.Image = null;
                    Cam5_Open();
                }
            }
            else if ((index1 == -1) && (index2 != -1) && (index3 != -1) && (index4 == -1) && (index5 == -1))    //cam2, 3만 켜져있을 경우      //01100
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame2 == null) || (frame3 == null)) { break; }
                    if ((index2 == -1) || (index3 == -1)) { break; }
                    Cam1_Camera_pb.Image = null;
                    Cam2_Open();
                    Cam3_Open();
                    Cam4_Camera_pb.Image = null;
                    Cam5_Camera_pb.Image = null;
                }
            }
            else if ((index1 == -1) && (index2 != -1) && (index3 == -1) && (index4 != -1) && (index5 == -1))    //cam2, 4만 켜져있을 경우      //01010
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame2 == null) || (frame4 == null)) { break; }
                    if ((index2 == -1) || (index4 == -1)) { break; }
                    Cam1_Camera_pb.Image = null;
                    Cam2_Open();
                    Cam3_Camera_pb.Image = null;
                    Cam4_Open();
                    Cam5_Camera_pb.Image = null;
                }
            }
            else if ((index1 == -1) && (index2 != -1) && (index3 == -1) && (index4 == -1) && (index5 != -1))    //cam2, 5만 켜져있을 경우      //01001
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame2 == null) || (frame5 == null)) { break; }
                    if ((index2 == -1) || (index5 == -1)) { break; }
                    Cam1_Camera_pb.Image = null;
                    Cam2_Open();
                    Cam3_Camera_pb.Image = null;
                    Cam4_Camera_pb.Image = null;
                    Cam5_Open();
                }
            }
            else if ((index1 == -1) && (index2 == -1) && (index3 != -1) && (index4 != -1) && (index5 == -1))    //cam3,4만 켜져있을 경우       //00110
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame3 == null) || (frame4 == null)) { break; }
                    if ((index3 == -1) || (index4 == -1)) { break; }
                    Cam1_Camera_pb.Image = null;
                    Cam2_Camera_pb.Image = null;
                    Cam3_Open();
                    Cam4_Open();
                    Cam5_Camera_pb.Image = null;
                }
            }
            else if ((index1 == -1) && (index2 == -1) && (index3 != -1) && (index4 == -1) && (index5 != -1))    //cam3, 5만 켜져있을 경우      //00101
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame3 == null) || (frame5 == null)) { break; }
                    if ((index3 == -1) || (index5 == -1)) { break; }
                    Cam1_Camera_pb.Image = null;
                    Cam2_Camera_pb.Image = null;
                    Cam3_Open();
                    Cam4_Camera_pb.Image = null;
                    Cam5_Open();
                }
            }
            else if ((index1 == -1) && (index2 == -1) && (index3 == -1) && (index4 != -1) && (index5 != -1))    //cam4, 5만 켜져있을 경우      //00011
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame4 == null) || (frame5 == null)) { break; }
                    if ((index4 == -1) || (index5 == -1)) { break; }
                    Cam1_Camera_pb.Image = null;
                    Cam2_Camera_pb.Image = null;
                    Cam3_Camera_pb.Image = null;
                    Cam4_Open();
                    Cam5_Open();
                }
            }
            else if ((index1 != -1) && (index2 != -1) && (index3 != -1) && (index4 == -1) && (index5 == -1))    //cam1, 2, 3만 켜져있을 경우   //11100
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame1 == null) || (frame2 == null) || (frame3 == null)) { break; }
                    if ((index1 == -1) || (index2 == -1) || (index3 == -1)) { break; }
                    Cam1_Open();
                    Cam2_Open();
                    Cam3_Open();
                    Cam4_Camera_pb.Image = null;
                    Cam5_Camera_pb.Image = null;
                }
            }
            else if ((index1 != -1) && (index2 != -1) && (index3 == -1) && (index4 != -1) && (index5 == -1))    //cam1, 2, 4만 켜져있을 경우   //11010
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame1 == null) || (frame2 == null) || (frame4 == null)) { break; }
                    if ((index1 == -1) || (index2 == -1) || (index4 == -1)) { break; }
                    Cam1_Open();
                    Cam2_Open();
                    Cam3_Camera_pb.Image = null;
                    Cam4_Open();
                    Cam5_Camera_pb.Image = null;
                }
            }
            else if ((index1 != -1) && (index2 != -1) && (index3 == -1) && (index4 == -1) && (index5 != -1))    //cam1, 2, 5만 켜져있을 경우   //11001
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame1 == null) || (frame2 == null) || (frame5 == null)) { break; }
                    if ((index1 == -1) || (index2 == -1) || (index5 == -1)) { break; }
                    Cam1_Open();
                    Cam2_Open();
                    Cam3_Camera_pb.Image = null;
                    Cam4_Camera_pb.Image = null;
                    Cam5_Open();
                }
            }
            else if ((index1 != -1) && (index2 == -1) && (index3 != -1) && (index4 != -1) && (index5 == -1))    //cam1, 3, 4만 켜져있을 경우   //10110
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame1 == null) || (frame3 == null) || (frame4 == null)) { break; }
                    if ((index1 == -1) || (index3 == -1) || (index4 == -1)) { break; }
                    Cam1_Open();
                    Cam2_Camera_pb.Image = null;
                    Cam3_Open();
                    Cam4_Open();
                    Cam5_Camera_pb.Image = null;
                }
            }
            else if ((index1 != -1) && (index2 == -1) && (index3 != -1) && (index4 == -1) && (index5 != -1))    //cam1, 3, 5만 켜져있을 경우   //10101
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame1 == null) || (frame3 == null) || (frame5 == null)) { break; }
                    if ((index1 == -1) || (index3 == -1) || (index5 == -1)) { break; }
                    Cam1_Open();
                    Cam2_Camera_pb.Image = null;
                    Cam3_Open();
                    Cam4_Camera_pb.Image = null;
                    Cam5_Open();
                }
            }
            else if ((index1 != -1) && (index2 == -1) && (index3 == -1) && (index4 != -1) && (index5 != -1))    //cam1, 4, 5만 켜져있을 경우   //10011
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame1 == null) || (frame4 == null) || (frame5 == null)) { break; }
                    if ((index1 == -1) || (index4 == -1) || (index5 == -1)) { break; }
                    Cam1_Open();
                    Cam2_Camera_pb.Image = null;
                    Cam3_Camera_pb.Image = null;
                    Cam4_Open();
                    Cam5_Open();
                }
            }
            else if ((index1 == -1) && (index2 != -1) && (index3 != -1) && (index4 != -1) && (index5 == -1))    //cam2, 3, 4만 켜져있을 경우   //01110
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame2 == null) || (frame3 == null) || (frame4 == null)) { break; }
                    if ((index2 == -1) || (index3 == -1) || (index4 == -1)) { break; }
                    Cam1_Camera_pb.Image = null;
                    Cam2_Open();
                    Cam3_Open();
                    Cam4_Open();
                    Cam5_Camera_pb.Image = null;
                }
            }
            else if ((index1 == -1) && (index2 != -1) && (index3 != -1) && (index4 == -1) && (index5 != -1))    //cam2, 3, 5만 켜져있을 경우   //01101
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame2 == null) || (frame3 == null) || (frame5 == null)) { break; }
                    if ((index2 == -1) || (index3 == -1) || (index5 == -1)) { break; }
                    Cam1_Camera_pb.Image = null;
                    Cam2_Open();
                    Cam3_Open();
                    Cam4_Camera_pb.Image = null;
                    Cam5_Open();
                }
            }
            else if ((index1 == -1) && (index2 != -1) && (index3 == -1) && (index4 != -1) && (index5 != -1))    //cam2, 4, 5만 켜져있을 경우   //01011
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame2 == null) || (frame4 == null) || (frame5 == null)) { break; }
                    if ((index2 == -1) || (index4 == -1) || (index5 == -1)) { break; }
                    Cam1_Camera_pb.Image = null;
                    Cam2_Open();
                    Cam3_Camera_pb.Image = null;
                    Cam4_Open();
                    Cam5_Open();
                }
            }
            else if ((index1 == -1) && (index2 == -1) && (index3 != -1) && (index4 != -1) && (index5 != -1))    //cam3, 4, 5만 켜져있을 경우   //00111
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame3 == null) || (frame4 == null) || (frame5 == null)) { break; }
                    if ((index3 == -1) || (index4 == -1) || (index5 == -1)) { break; }
                    Cam1_Camera_pb.Image = null;
                    Cam2_Camera_pb.Image = null;
                    Cam3_Open();
                    Cam4_Open();
                    Cam5_Open();
                }
            }
            else if ((index1 != -1) && (index2 != -1) && (index3 != -1) && (index4 != -1) && (index5 == -1))    //cam1, 2, 3, 4만 켜져있을 경우//11110
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame1 == null) || (frame2 == null) || (frame3 == null) || (frame4 == null)) { break; }
                    if ((index1 == -1) || (index2 == -1) || (index3 == -1) || (index4 == -1)) { break; }
                    Cam1_Open();
                    Cam2_Open();
                    Cam3_Open();
                    Cam4_Open();
                    Cam5_Camera_pb.Image = null;
                }
            }
            else if ((index1 != -1) && (index2 != -1) && (index3 != -1) && (index4 == -1) && (index5 != -1))    //cam1, 2, 3, 5만 켜져있을 경우//11101
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame1 == null) || (frame2 == null) || (frame3 == null) || (frame5 == null)) { break; }
                    if ((index1 == -1) || (index2 == -1) || (index3 == -1) || (index5 == -1)) { break; }
                    Cam1_Open();
                    Cam2_Open();
                    Cam3_Open();
                    Cam4_Camera_pb.Image = null;
                    Cam5_Open();
                }
            }
            else if ((index1 != -1) && (index2 != -1) && (index3 == -1) && (index4 != -1) && (index5 != -1))    //cam1, 2, 4, 5만 켜져있을 경우//11011
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame1 == null) || (frame2 == null) || (frame4 == null) || (frame5 == null)) { break; }
                    if ((index1 == -1) || (index2 == -1) || (index4 == -1) || (index5 == -1)) { break; }
                    Cam1_Open();
                    Cam2_Open();
                    Cam3_Camera_pb.Image = null;
                    Cam4_Open();
                    Cam5_Open();
                }
            }
            else if ((index1 != -1) && (index2 == -1) && (index3 != -1) && (index4 != -1) && (index5 != -1))    //cam1, 3, 4, 5만 켜져있는 경우//10111
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame1 == null) || (frame3 == null) || (frame4 == null) || (frame5 == null)) { break; }
                    if ((index1 == -1) || (index3 == -1) || (index4 == -1) || (index5 == -1)) { break; }
                    Cam1_Open();
                    Cam2_Camera_pb.Image = null;
                    Cam3_Open();
                    Cam4_Open(); 
                    Cam5_Open();
                }
            }
            else if ((index1 == -1) && (index2 != -1) && (index3 != -1) && (index4 != -1) && (index5 != -1))    //cam2, 3, 4, 5만 켜져있을 경우//01111
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame2 == null) || (frame3 == null) || (frame4 == null) || (frame5 == null)) { break; }
                    if ((index2 == -1) || (index3 == -1) || (index4 == -1) || (index5 == -1)) { break; }
                    Cam1_Camera_pb.Image = null;
                    Cam2_Open();
                    Cam3_Open();
                    Cam4_Open(); 
                    Cam5_Open();
                }
            }
            else if ((index1 != -1) && (index2 != -1) && (index3 != -1) && (index4 != -1) && (index5 != -1))    //cam이 모두 켜져있을 경우      //11111
            {
                while (Cv2.WaitKey(33) != 27)
                {
                    if ((frame1 == null) || (frame2 == null) || (frame3 == null) || (frame4 == null) || (frame5 == null)) { break; }
                    if ((index1 == -1) || (index2 == -1) || (index3 == -1) || (index4 == -1) || (index5 == -1)) { break; }
                    Cam1_Open();
                    Cam2_Open();
                    Cam3_Open();
                    Cam4_Open();
                    Cam5_Open();
                }
            }
        }

        private void Cam1_CheckPoint1_Click(object sender, EventArgs e) //1.Check Point1
        {
            Cam1_Check1_openFile.ShowDialog();  //OpenFileDialog 창열기
        }

        private void Cam1_CheckPoint2_Click(object sender, EventArgs e) //1.Check Point2
        {
            Cam1_Check2_openFile.ShowDialog();  //OpenFileDialog 창열기
        }

        private void Cam2_CheckPoint1_Click(object sender, EventArgs e) //2.Check Point1
        {
            Cam2_Check1_openFile.ShowDialog();  //OpenFileDialog 창열기
        }

        private void Cam2_CheckPoint2_Click(object sender, EventArgs e) //2.Check Point2
        {
            Cam2_Check2_openFile.ShowDialog();  //OpenFileDialog 창열기
        }

        private void Cam3_CheckPoint1_Click(object sender, EventArgs e) //3.Check Point1
        {
            Cam3_Check1_openFile.ShowDialog();  //OpenFileDialog 창열기
        }

        private void Cam3_CheckPoint2_Click(object sender, EventArgs e) //3.CheckPoint2
        {
            Cam3_Check2_openFile.ShowDialog();  //OpenFileDialog 창열기
        }

        private void Cam4_CheckPoint1_Click(object sender, EventArgs e) //4.Check Point1
        {
            Cam4_Check1_openFile.ShowDialog();  //OpenFileDialog 창열기
        }

        private void Cam4_CheckPoint2_Click(object sender, EventArgs e) //4.Check Point2
        {
            Cam4_Check2_openFile.ShowDialog();  //OpenFileDialog 창열기
        }

        private void Cam5_CheckPoint1_Click(object sender, EventArgs e) //5.Check Point1
        {
            Cam5_Check1_openFile.ShowDialog();  //OpenFileDialog 창열기
        }

        private void Cam5_CheckPoint2_Click(object sender, EventArgs e) //5.Check Point2
        {
            Cam5_Check2_openFile.ShowDialog();  //OpenFileDialog 창열기
        }

        private void Cam1_Check1_openFile_FileOk(object sender, CancelEventArgs e)  //1.openFileDialog1
        {
            Cam1_AngleCheck1_cnt = 2; Cam1_AngleCheck1_Click(sender, e);    //AngleCheck1_cnt의 값을 2로 설정, AngleCheck1 버튼 클릭
        }

        private void Cam1_Check2_openFile_FileOk(object sender, CancelEventArgs e)  //1.openFileDialog2
        {
            Cam1_AngleCheck2_cnt = 2; Cam1_AngleCheck2_Click(sender, e);    //AngleCheck2_cnt의 값을 2로 설정, AngleCheck2 버튼 클릭
        }

        private void Cam2_Check1_openFile_FileOk(object sender, CancelEventArgs e)  //2.openFileDialog1
        {
            Cam2_AngleCheck1_cnt = 2; Cam2_AngleCheck1_Click(sender, e);    //AngleCheck1_cnt의 값을 2로 설정, AngleCheck1 버튼 클릭
        }

        private void Cam2_Check2_openFile_FileOk(object sender, CancelEventArgs e)  //2.openFileDialog2
        {
            Cam2_AngleCheck2_cnt = 2; Cam2_AngleCheck2_Click(sender, e);    //AngleCheck2_cnt의 값을 2로 설정, AngleCheck2 버튼 클릭
        }

        private void Cam3_Check1_openFile_FileOk(object sender, CancelEventArgs e)  //3.openFileDialog1
        {
            Cam3_AngleCheck1_cnt = 2; Cam3_AngleCheck1_Click(sender, e);    //AngleCheck1_cnt의 값을 2로 설정, AngleCheck1 버튼 클릭
        }

        private void Cam3_Check2_openFile_FileOk(object sender, CancelEventArgs e)  //3.openFileDialog2
        {
            Cam3_AngleCheck2_cnt = 2; Cam3_AngleCheck2_Click(sender, e);    //AngleCheck2_cnt의 값을 2로 설정, AngleCheck2 버튼 클릭
        }

        private void Cam4_Check1_openFile_FileOk(object sender, CancelEventArgs e)  //4.openFileDialog1
        {
            Cam4_AngleCheck1_cnt = 2; Cam4_AngleCheck1_Click(sender, e);    //AngleCheck1_cnt의 값을 2로 설정, AngleCheck1 버튼 클릭
        }

        private void Cam4_Check2_openFile_FileOk(object sender, CancelEventArgs e)  //4.openFileDialog2
        {
            Cam4_AngleCheck2_cnt = 2; Cam4_AngleCheck2_Click(sender, e);    //AngleCheck2_cnt의 값을 2로 설정, AngleCheck2 버튼 클릭
        }

        private void Cam5_Check1_openFile_FileOk(object sender, CancelEventArgs e)  //5.openFileDialog1
        {
            Cam5_AngleCheck1_cnt = 2; Cam5_AngleCheck1_Click(sender, e);    //AngleCheck1_cnt의 값을 2로 설정, AngleCheck1 버튼 클릭
        }

        private void Cam5_Check2_openFile_FileOk(object sender, CancelEventArgs e)  //5.openFileDialog2
        {
            Cam5_AngleCheck2_cnt = 2; Cam5_AngleCheck2_Click(sender, e);    //AngleCheck2_cnt의 값을 2로 설정, AngleCheck2 버튼 클릭
        }

        private void Cam1_Save_Click(object sender, EventArgs e)    //1.Save
        {
            string folderpath = @Wheretosave.SelectedPath + "\\Cam1";   //폴더 이름 설정
            DateTime date = DateTime.Now;
            if (!Directory.Exists(folderpath)) { Directory.CreateDirectory(folderpath); }   //폴더가 존재하지 않을 경우 폴더 생성
            if(Cam1_Camera_pb.Image != null)
            {
                Bitmap Camera_pb = Save.Image_save(CheckPoint1group.Location.X + Cam1_Camera_pn.Location.X, CheckPoint1group.Location.Y + Cam1_Camera_pn.Location.Y);
                Camera_pb.Save(folderpath + "\\" + Save.save1(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp); //Camera_pb에 이미지가 있을 경우 저장
            }
            if(Cam1_Check1_pb.Image != null)   //Check1_pb에 이미지가 있을 경우 저장
            {
                Bitmap origin = new Bitmap(picture_path + "\\resize_Cam1_Check1_pb.jpeg");  //원본 이미지
                origin = Save.Origin_line(origin, XY[0], XY[1], center[0], center[1], center[2], center[3]);
                origin.Save(folderpath + "\\" + Save.save2_1(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //원본 이미지 저장
                Cam1_Check1_pb.Image.Save(folderpath + "\\" + Save.save2_2(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //이진화 된 이미지 저장
            }
            if (Cam1_Check2_pb.Image != null)  //Check2_pb에 이미지가 있을 경우 저장
            {
                Bitmap origin = new Bitmap(picture_path + "\\resize_Cam1_Check2_pb.jpeg");  //원본 이미지
                origin = Save.Origin_line(origin, XY[2], XY[3], center[5], center[6], center[7], center[8]);
                origin.Save(folderpath + "\\" + Save.save3_1(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //원본 이미지 저장
                Cam1_Check2_pb.Image.Save(folderpath + "\\" + Save.save3_2(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //이진화 된 이미지 저장
            }
            MessageBox.Show("저장되었습니다.");
        }

        private void Cam2_Save_Click(object sender, EventArgs e)    //2.Save
        {
            string folderpath = @Wheretosave.SelectedPath + "\\Cam2";   //폴더 이름 설정
            DateTime date = DateTime.Now;
            if (!Directory.Exists(folderpath)) { Directory.CreateDirectory(folderpath); }   //폴더가 존재하지 않을 경우 폴더 생성
            if (Cam2_Camera_pb.Image != null)
            {
                Bitmap Camera_pb = Save.Image_save(CheckPoint2group.Location.X + Cam2_Camera_pn.Location.X, CheckPoint2group.Location.Y + Cam2_Camera_pn.Location.Y);
                Camera_pb.Save(folderpath + "\\" + Save.save1(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp); //Camera_pb에 이미지가 있을 경우 저장
            }
            if (Cam2_Check1_pb.Image != null)   //Check1_pb에 이미지가 있을 경우 저장
            {
                Bitmap origin = new Bitmap(picture_path + "\\resize_Cam2_Check1_pb.jpeg");  //원본 이미지
                origin = Save.Origin_line(origin, XY[4], XY[5], center[10], center[11], center[12], center[13]);
                origin.Save(folderpath + "\\" + Save.save2_1(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //원본 이미지 저장
                Cam2_Check1_pb.Image.Save(folderpath + "\\" + Save.save2_2(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //이진화 된 이미지 저장
            }
            if (Cam2_Check2_pb.Image != null)  //Check2_pb에 이미지가 있을 경우 저장
            {
                Bitmap origin = new Bitmap(picture_path + "\\resize_Cam2_Check2_pb.jpeg");  //원본 이미지
                origin = Save.Origin_line(origin, XY[6], XY[7], center[15], center[16], center[17], center[18]);
                origin.Save(folderpath + "\\" + Save.save3_1(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //원본 이미지 저장
                Cam2_Check2_pb.Image.Save(folderpath + "\\" + Save.save3_2(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //이진화 된 이미지 저장
            }
            MessageBox.Show("저장되었습니다.");
        }

        private void Cam3_Save_Click(object sender, EventArgs e)    //3.Save
        {
            string folderpath = @Wheretosave.SelectedPath + "\\Cam3";   //폴더 이름 설정
            DateTime date = DateTime.Now;
            if (!Directory.Exists(folderpath)) { Directory.CreateDirectory(folderpath); }   //폴더가 존재하지 않을 경우 폴더 생성
            if (Cam3_Camera_pb.Image != null)
            {
                Bitmap Camera_pb = Save.Image_save(CheckPoint3group.Location.X + Cam3_Camera_pn.Location.X, CheckPoint3group.Location.Y + Cam3_Camera_pn.Location.Y);
                Camera_pb.Save(folderpath + "\\" + Save.save1(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp); //Camera_pb에 이미지가 있을 경우 저장
            }
            if (Cam3_Check1_pb.Image != null)   //Check1_pb에 이미지가 있을 경우 저장
            {
                Bitmap origin = new Bitmap(picture_path + "\\resize_Cam3_Check1_pb.jpeg");  //원본 이미지
                origin = Save.Origin_line(origin, XY[8], XY[9], center[20], center[21], center[22], center[23]);
                origin.Save(folderpath + "\\" + Save.save2_1(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //원본 이미지 저장
                Cam3_Check1_pb.Image.Save(folderpath + "\\" + Save.save2_2(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //이진화 된 이미지 저장
            }
            if (Cam3_Check2_pb.Image != null)  //Check2_pb에 이미지가 있을 경우 저장
            {
                Bitmap origin = new Bitmap(picture_path + "\\resize_Cam3_Check2_pb.jpeg");  //원본 이미지
                origin = Save.Origin_line(origin, XY[10], XY[11], center[25], center[26], center[27], center[28]);
                origin.Save(folderpath + "\\" + Save.save3_1(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //원본 이미지 저장
                Cam3_Check2_pb.Image.Save(folderpath + "\\" + Save.save3_2(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //이진화 된 이미지 저장
            }
            MessageBox.Show("저장되었습니다.");
        }

        private void Cam4_Save_Click(object sender, EventArgs e)    //4.Save
        {
            string folderpath = @Wheretosave.SelectedPath + "\\Cam4";   //폴더 이름 설정
            DateTime date = DateTime.Now;
            if (!Directory.Exists(folderpath)) { Directory.CreateDirectory(folderpath); }   //폴더가 존재하지 않을 경우 폴더 생성
            if (Cam4_Camera_pb.Image != null)
            {
                Bitmap Camera_pb = Save.Image_save(CheckPoint4group.Location.X + Cam4_Camera_pn.Location.X, CheckPoint4group.Location.Y + Cam4_Camera_pn.Location.Y);
                Camera_pb.Save(folderpath + "\\" + Save.save1(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp); //Camera_pb에 이미지가 있을 경우 저장
            }
            if (Cam4_Check1_pb.Image != null)   //Check1_pb에 이미지가 있을 경우 저장
            {
                Bitmap origin = new Bitmap(picture_path + "\\resize_Cam4_Check1_pb.jpeg");  //원본 이미지
                origin = Save.Origin_line(origin, XY[12], XY[13], center[30], center[31], center[32], center[33]);
                origin.Save(folderpath + "\\" + Save.save2_1(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //원본 이미지 저장
                Cam4_Check1_pb.Image.Save(folderpath + "\\" + Save.save2_2(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //이진화 된 이미지 저장
            }
            if (Cam4_Check2_pb.Image != null)  //Check2_pb에 이미지가 있을 경우 저장
            {
                Bitmap origin = new Bitmap(picture_path + "\\resize_Cam4_Check2_pb.jpeg");  //원본 이미지
                origin = Save.Origin_line(origin, XY[14], XY[15], center[35], center[36], center[37], center[38]);
                origin.Save(folderpath + "\\" + Save.save3_1(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //원본 이미지 저장
                Cam4_Check2_pb.Image.Save(folderpath + "\\" + Save.save3_2(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //이진화 된 이미지 저장
            }
            MessageBox.Show("저장되었습니다.");
        }

        private void Cam5_Save_Click(object sender, EventArgs e)    //5.Save
        {
            string folderpath = @Wheretosave.SelectedPath + "\\Cam5";   //폴더 이름 설정
            DateTime date = DateTime.Now;
            if (!Directory.Exists(folderpath)) { Directory.CreateDirectory(folderpath); }   //폴더가 존재하지 않을 경우 폴더 생성
            if (Cam5_Camera_pb.Image != null)
            {
                Bitmap Camera_pb = Save.Image_save(CheckPoint5group.Location.X + Cam5_Camera_pn.Location.X, CheckPoint5group.Location.Y + Cam5_Camera_pn.Location.Y);
                Camera_pb.Save(folderpath + "\\" + Save.save1(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp); //Camera_pb에 이미지가 있을 경우 저장
            }
            if (Cam5_Check1_pb.Image != null)   //Check1_pb에 이미지가 있을 경우 저장
            {
                Bitmap origin = new Bitmap(picture_path + "\\resize_Cam5_Check1_pb.jpeg");  //원본 이미지
                origin = Save.Origin_line(origin, XY[16], XY[17], center[40], center[41], center[42], center[43]);
                origin.Save(folderpath + "\\" + Save.save2_1(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //원본 이미지 저장
                Cam5_Check1_pb.Image.Save(folderpath + "\\" + Save.save2_2(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //이진화 된 이미지 저장
            }
            if (Cam5_Check2_pb.Image != null)   //Check2_pb에 이미지가 있을 경우 저장
            {
                Bitmap origin = new Bitmap(picture_path + "\\resize_Cam5_Check2_pb.jpeg");  //원본 이미지
                origin = Save.Origin_line(origin, XY[18], XY[19], center[45], center[46], center[47], center[48]);
                origin.Save(folderpath + "\\" + Save.save3_1(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //원본 이미지 저장
                Cam5_Check2_pb.Image.Save(folderpath + "\\" + Save.save3_2(date) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);  //이진화 된 이미지 저장
            }
            MessageBox.Show("저장되었습니다.");
        }

        private void Btn_Wheretosave_Click(object sender, EventArgs e)  //where to save
        {
            if (Wheretosave.ShowDialog() == DialogResult.OK)    //FolderBrowserDialog에서 폴더를 선택한 경우
            {
                txt_Wheretosave.Text = Wheretosave.SelectedPath;    //선택한 위치 textBox에 입력
            }
        }

        private void Cam1_mag_SelectedItemChanged(object sender, EventArgs e)   //Cam1 배율
        {
            Bitmap pic_mag = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame1));   //Mat형식 Bitmap으로 변환
            Cam1_Camera_pb.Image = Position.Mag_Image(pic_mag, (int)(pic_mag.Width * TXT.TXT_to_Double(Cam1_mag.Text) / 100), (int)(pic_mag.Height * TXT.TXT_to_Double(Cam1_mag.Text) / 100)); //배율 조정
        }

        private void Cam2_mag_SelectedItemChanged(object sender, EventArgs e)   //Cam2 배율
        {
            Bitmap pic_mag = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame2));   //Mat형식 Bitmap으로 변환
            pic_mag = Position.Mag_Image(pic_mag, (int)(pic_mag.Width * TXT.TXT_to_Double(Cam2_mag.Text) / 100), (int)(pic_mag.Height * TXT.TXT_to_Double(Cam2_mag.Text) / 100)); //배율 조정
            Cam2_Camera_pb.Image = pic_mag; //배율 조정한 이미지 
        }

        private void Cam3_mag_SelectedItemChanged(object sender, EventArgs e)   //Cam3 배율
        {
            Bitmap pic_mag = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame3));   //Mat형식 Bitmap으로 변환
            pic_mag = Position.Mag_Image(pic_mag, (int)(pic_mag.Width * TXT.TXT_to_Double(Cam3_mag.Text) / 100), (int)(pic_mag.Height * TXT.TXT_to_Double(Cam3_mag.Text) / 100)); //배율 조정
            Cam3_Camera_pb.Image = pic_mag; //배율 조정한 이미지 
        }

        private void Cam4_mag_SelectedItemChanged(object sender, EventArgs e)   //Cam4 배율
        {
            Bitmap pic_mag = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame4));   //Mat형식 Bitmap으로 변환
            pic_mag = Position.Mag_Image(pic_mag, (int)(pic_mag.Width * TXT.TXT_to_Double(Cam4_mag.Text) / 100), (int)(pic_mag.Height * TXT.TXT_to_Double(Cam4_mag.Text) / 100)); //배율 조정
            Cam4_Camera_pb.Image = pic_mag; //배율 조정한 이미지 
        }

        private void Cam5_mag_SelectedItemChanged(object sender, EventArgs e)   //Cam5 배율
        {
            Bitmap pic_mag = new Bitmap(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame5));   //Mat형식 Bitmap으로 변환
            pic_mag = Position.Mag_Image(pic_mag, (int)(pic_mag.Width * TXT.TXT_to_Double(Cam5_mag.Text) / 100), (int)(pic_mag.Height * TXT.TXT_to_Double(Cam5_mag.Text) / 100)); //배율 조정
            Cam5_Camera_pb.Image = pic_mag; //배율 조정한 이미지 
        }

        private void Cam1_Check1_X_ValueChanged(object sender, EventArgs e) { Cam1_Camera_pb.Left = Position.X(Cam1_X.Value.ToString(), Cam1_Camera_pb.Image); }   //Cam1_Check1_X좌표 이동

        private void Cam1_Check1_Y_ValueChanged(object sender, EventArgs e) { Cam1_Camera_pb.Top = Position.Y(Cam1_Y.Value.ToString(), Cam1_Camera_pb.Image); }    //Cam1_Check1_Y좌표 이동

        private void Cam2_Check1_X_ValueChanged(object sender, EventArgs e) { Cam2_Camera_pb.Left = Position.X(Cam2_X.Value.ToString(), Cam2_Camera_pb.Image); }   //Cam2_Check1_X좌표 이동

        private void Cam2_Check1_Y_ValueChanged(object sender, EventArgs e) { Cam2_Camera_pb.Top = Position.Y(Cam2_Y.Value.ToString(), Cam2_Camera_pb.Image); }    //Cam2_Check1_Y좌표 이동

        private void Cam3_Check1_X_ValueChanged(object sender, EventArgs e) { Cam3_Camera_pb.Left = Position.X(Cam3_X.Value.ToString(), Cam3_Camera_pb.Image); }   //Cam3_Check1_X좌표 이동

        private void Cam3_Check1_Y_ValueChanged(object sender, EventArgs e) { Cam3_Camera_pb.Top = Position.Y(Cam3_Y.Value.ToString(), Cam3_Camera_pb.Image); }    //Cam3_Check1_Y좌표 이동

        private void Cam4_Check1_X_ValueChanged(object sender, EventArgs e) { Cam4_Camera_pb.Left = Position.X(Cam4_X.Value.ToString(), Cam4_Camera_pb.Image); }   //Cam4_Check1_X좌표 이동

        private void Cam4_Check1_Y_ValueChanged(object sender, EventArgs e) { Cam4_Camera_pb.Top = Position.Y(Cam4_Y.Value.ToString(), Cam4_Camera_pb.Image); }    //Cam4_Check1_Y좌표 이동

        private void Cam5_Check1_X_ValueChanged(object sender, EventArgs e) { Cam5_Camera_pb.Left = Position.X(Cam5_X.Value.ToString(), Cam5_Camera_pb.Image); }   //Cam5_Check1_X좌표 이동

        private void Cam5_Check1_Y_ValueChanged(object sender, EventArgs e) { Cam5_Camera_pb.Top = Position.Y(Cam5_Y.Value.ToString(), Cam5_Camera_pb.Image); }    //Cam5_Check1_Y좌표 이동

        private void CamSelect1_cbb_SelectedIndexChanged(object sender, EventArgs e) { index1 = CamSelect1_cbb.SelectedIndex - 1; } //카메라 값은 0번부터 시작이므로 index의 입력될 값에 -1

        private void CamSelect2_cbb_SelectedIndexChanged(object sender, EventArgs e) { index2 = CamSelect2_cbb.SelectedIndex - 1; }

        private void CamSelect3_cbb_SelectedIndexChanged(object sender, EventArgs e) { index3 = CamSelect3_cbb.SelectedIndex - 1; }

        private void CamSelect4_cbb_SelectedIndexChanged(object sender, EventArgs e) { index4 = CamSelect4_cbb.SelectedIndex - 1; }

        private void CamSelect5_cbb_SelectedIndexChanged(object sender, EventArgs e) { index5 = CamSelect5_cbb.SelectedIndex - 1; }
        
        private void Btn_Reset_Click(object sender, EventArgs e)    //Reset
        {   //Camera 선택 없음으로 모두 변경
            CamSelect1_cbb.SelectedIndex = 0; CamSelect2_cbb.SelectedIndex = 0; CamSelect3_cbb.SelectedIndex = 0; CamSelect4_cbb.SelectedIndex = 0; CamSelect5_cbb.SelectedIndex = 0;
            
            //각도 값 text 모두 0.00으로 설정
            Cam1_Check1_txt.Text = "0"; Cam1_Check2_txt.Text = "0"; Cam1_Anglediff.Text = "0.00";
            Cam2_Check1_txt.Text = "0"; Cam2_Check2_txt.Text = "0"; Cam2_Anglediff.Text = "0.00";
            Cam3_Check1_txt.Text = "0"; Cam3_Check2_txt.Text = "0"; Cam3_Anglediff.Text = "0.00";
            Cam4_Check1_txt.Text = "0"; Cam4_Check2_txt.Text = "0"; Cam4_Anglediff.Text = "0.00";
            Cam5_Check1_txt.Text = "0"; Cam5_Check2_txt.Text = "0"; Cam5_Anglediff.Text = "0.00";
            //모든 cnt 값 0으로 변경
            Cam1_AngleCheck1_cnt = 0; Cam1_AngleCheck2_cnt = 0;
            Cam2_AngleCheck1_cnt = 0; Cam2_AngleCheck2_cnt = 0;
            Cam3_AngleCheck1_cnt = 0; Cam3_AngleCheck2_cnt = 0;
            Cam4_AngleCheck1_cnt = 0; Cam4_AngleCheck2_cnt = 0;
            Cam5_AngleCheck1_cnt = 0; Cam5_AngleCheck2_cnt = 0;
            Cam1_AngleCheck1_Value_cnt = 0; Cam1_AngleCheck2_Value_cnt = 0;
            Cam2_AngleCheck1_Value_cnt = 0; Cam2_AngleCheck2_Value_cnt = 0;
            Cam3_AngleCheck1_Value_cnt = 0; Cam3_AngleCheck2_Value_cnt = 0;
            Cam4_AngleCheck1_Value_cnt = 0; Cam4_AngleCheck2_Value_cnt = 0;
            Cam5_AngleCheck1_Value_cnt = 0; Cam5_AngleCheck2_Value_cnt = 0;
            //이동 값 0으로 설정
            Cam1_X.Value = 0; Cam1_Y.Value = 0; Cam1_mag.Text = "100";
            Cam2_X.Value = 0; Cam2_Y.Value = 0; Cam2_mag.Text = "100";
            Cam3_X.Value = 0; Cam3_Y.Value = 0; Cam3_mag.Text = "100";
            Cam4_X.Value = 0; Cam4_Y.Value = 0; Cam4_mag.Text = "100";
            Cam5_X.Value = 0; Cam5_Y.Value = 0; Cam5_mag.Text = "100";
            //모든 pictureBox의 이미지 지우기
            Cam1_Camera_pb.Image = null; Cam1_Check1_pb.Image = null; Cam1_Check2_pb.Image = null;
            Cam2_Camera_pb.Image = null; Cam2_Check1_pb.Image = null; Cam2_Check2_pb.Image = null;
            Cam3_Camera_pb.Image = null; Cam3_Check1_pb.Image = null; Cam3_Check2_pb.Image = null;
            Cam4_Camera_pb.Image = null; Cam4_Check1_pb.Image = null; Cam4_Check2_pb.Image = null;
            Cam5_Camera_pb.Image = null; Cam5_Check1_pb.Image = null; Cam5_Check2_pb.Image = null;
            //카메라 영상 모두 지우기
            if (cam1 != null) { frame1 = null; }
            if (cam2 != null) { frame2 = null; }
            if (cam3 != null) { frame3 = null; }
            if (cam4 != null) { frame4 = null; }
            if (cam5 != null) { frame5 = null; }
        }

        private void Cam1_Check1_Bar_Scroll(object sender, EventArgs e) //Cam1_Check1 임계값
        {
            Cam1_Check1_Value = Cam1_Check1_Bar.Value;  //숫자를 입력을 한 값과 트랙바로 이동 한 값 일치
            Cam1_Check1_txt.Text = Cam1_Check1_Value.ToString();    //트랙바로 이동 한 값을 Cam1_Check1_txt에 띄우기
        }

        private void Cam1_Check2_Bar_Scroll(object sender, EventArgs e) //Cam1_Check2 임계값
        {
            Cam1_Check2_Value = Cam1_Check2_Bar.Value;  //숫자를 입력을 한 값과 트랙바로 이동 한 값 일치
            Cam1_Check2_txt.Text = Cam1_Check2_Value.ToString();    //트랙바로 이동 한 값을 Cam1_Check2_txt에 띄우기
        }

        private void Cam2_Check1_Bar_Scroll(object sender, EventArgs e) //Cam2_Check1 임계값
        {
            Cam2_Check1_Value = Cam2_Check1_Bar.Value;  //숫자를 입력을 한 값과 트랙바로 이동 한 값 일치
            Cam2_Check1_txt.Text = Cam2_Check1_Value.ToString();    //트랙바로 이동 한 값을 Cam2_Check1_txt에 띄우기
        }

        private void Cam2_Check2_Bar_Scroll(object sender, EventArgs e) //Cam2_Check2 임계값
        {
            Cam2_Check2_Value = Cam2_Check2_Bar.Value;  //숫자를 입력을 한 값과 트랙바로 이동 한 값 일치
            Cam2_Check2_txt.Text = Cam2_Check2_Value.ToString();    //트랙바로 이동 한 값을 Cam2_Check2_txt에 띄우기
        }

        private void Cam3_Check1_Bar_Scroll(object sender, EventArgs e) //Cam3_Check1 임계값
        {
            Cam3_Check1_Value = Cam3_Check1_Bar.Value;  //숫자를 입력을 한 값과 트랙바로 이동 한 값 일치
            Cam3_Check1_txt.Text = Cam3_Check1_Value.ToString();    //트랙바로 이동 한 값을 Cam3_Check1_txt에 띄우기
        }

        private void Cam3_Check2_Bar_Scroll(object sender, EventArgs e) //Cam3_Check2 임계값
        {
            Cam3_Check2_Value = Cam3_Check2_Bar.Value;  //숫자를 입력을 한 값과 트랙바로 이동 한 값 일치
            Cam3_Check2_txt.Text = Cam3_Check2_Value.ToString();    //트랙바로 이동 한 값을 Cam3_Check2_txt에 띄우기
        }

        private void Cam4_Check1_Bar_Scroll(object sender, EventArgs e) //Cam4_Check1 임계값
        {
            Cam4_Check1_Value = Cam4_Check1_Bar.Value;  //숫자를 입력을 한 값과 트랙바로 이동 한 값 일치
            Cam4_Check1_txt.Text = Cam4_Check1_Value.ToString();    //트랙바로 이동 한 값을 Cam4_Check1_txt에 띄우기
        }

        private void Cam4_Check2_Bar_Scroll(object sender, EventArgs e) //Cam4_Check2 임계값
        {
            Cam4_Check2_Value = Cam4_Check2_Bar.Value;  //숫자를 입력을 한 값과 트랙바로 이동 한 값 일치
            Cam4_Check2_txt.Text = Cam4_Check2_Value.ToString();    //트랙바로 이동 한 값을 Cam4_Check2_txt에 띄우기
        }

        private void Cam5_Check1_Bar_Scroll(object sender, EventArgs e) //Cam5_Check1 임계값
        {
            Cam5_Check1_Value = Cam5_Check1_Bar.Value;  //숫자를 입력을 한 값과 트랙바로 이동 한 값 일치
            Cam5_Check1_txt.Text = Cam5_Check1_Value.ToString();    //트랙바로 이동 한 값을 Cam5_Check1_txt에 띄우기
        }

        private void Cam5_Check2_Bar_Scroll(object sender, EventArgs e) //Cam5_Check2 임계값
        {
            Cam5_Check2_Value = Cam5_Check2_Bar.Value;  //숫자를 입력을 한 값과 트랙바로 이동 한 값 일치
            Cam5_Check2_txt.Text = Cam5_Check2_Value.ToString();    //트랙바로 이동 한 값을 Cam5_Check2_txt에 띄우기
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)   //키보드가 눌렸을 때 숫자만 입력받기
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '-') //숫자나 '-'를 입력했을 경우 입력
            { e.Handled = true; }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cameraDevices = new List<DsDevice>();
            GetCameraList();    //설치된 카메라 목록 생성
            foreach (var cameraDevice in cameraDevices) //ComboBox에 설치되어있는 카메라 목록 추가
            {
                CamSelect1_cbb.Items.Add(cameraDevice.Name);
                CamSelect2_cbb.Items.Add(cameraDevice.Name);
                CamSelect3_cbb.Items.Add(cameraDevice.Name);
                CamSelect4_cbb.Items.Add(cameraDevice.Name);
                CamSelect5_cbb.Items.Add(cameraDevice.Name);
            }
            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)   //Form이 종료될때 실행
        {
            Cv2.DestroyAllWindows();
            Environment.Exit(0);
            if (cam1 != null) { frame1.Dispose(); cam1.Release(); } //Cam이 켜져 있을 경우 끄기
            if (cam2 != null) { frame2.Dispose(); cam2.Release(); }
            if (cam3 != null) { frame3.Dispose(); cam3.Release(); }
            if (cam4 != null) { frame4.Dispose(); cam4.Release(); }
            if (cam5 != null) { frame5.Dispose(); cam5.Release(); }
        }
    }
}
