using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using AForge.Video.DirectShow;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FaceTetection
{
    public partial class Form1 : Form
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoDevice;
        private VideoCapabilities[] videoCapabilities;
        private VideoCapabilities[] snapshotCapabilities;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count != 0)
            {
                foreach (FilterInfo device in videoDevices)
                {
                    cmbCamera.Items.Add(device.Name);
                }
            }
            else
            {
                cmbCamera.Items.Add("没有找到摄像头");
            }

            cmbCamera.SelectedIndex = 0;
        }

        private void cmbCamera_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (videoDevices.Count != 0)
            {
                videoDevice = new VideoCaptureDevice(videoDevices[cmbCamera.SelectedIndex].MonikerString);
                GetDeviceResolution(videoDevice);
            }
        }

        private void GetDeviceResolution(VideoCaptureDevice videoCaptureDevice)
        {
            cmbResolution.Items.Clear();
            videoCapabilities = videoCaptureDevice.VideoCapabilities;
            foreach (VideoCapabilities capabilty in videoCapabilities)
            {
                cmbResolution.Items.Add($"{capabilty.FrameSize.Width} x {capabilty.FrameSize.Height}");
            }
            cmbResolution.SelectedIndex = 0;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (videoDevice != null)
            {
                if ((videoCapabilities != null) && (videoCapabilities.Length != 0))
                {
                    videoDevice.VideoResolution = videoCapabilities[cmbResolution.SelectedIndex];

                    vispShoot.VideoSource = videoDevice;
                    vispShoot.Start();
                    EnableControlStatus(false);
                }
            }
        }

        private void EnableControlStatus(bool status)
        {
            cmbCamera.Enabled = status;
            cmbResolution.Enabled = status;
            btnConnect.Enabled = status;
            btnShoot.Enabled = !status;
            btnDisconnect.Enabled = !status;
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            DisConnect();
            EnableControlStatus(true);
        }

        private void DisConnect()
        {
            if (vispShoot.VideoSource != null)
            {
                vispShoot.SignalToStop();
                vispShoot.WaitForStop();
                vispShoot.VideoSource = null;
            }
        }

        private void btnShoot_Click(object sender, EventArgs e)
        { 
            /** 拍照 开始 */
            Bitmap img = vispShoot.GetCurrentVideoFrame();
            picbPreview.Image = img;
            this.textBox1.Text += "(" + DateTime.Now.ToLocalTime().ToString() + ")" + "已拍照完成." + "\r\n";
            /** 拍照 结束 */

            /** 图片转为base64 开始 */
            MemoryStream ms = new MemoryStream();
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            byte[] arr = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(arr, 0, (int)ms.Length);
            ms.Close();
            String base64 = Convert.ToBase64String(arr);
            this.textBox1.Text += "(" + DateTime.Now.ToLocalTime().ToString() + ")" + "转码完成." + "\r\n";
            String stringBase64 = "data:image/jpeg;base64," + base64;
            //this.textBox1.Text += "(" + DateTime.Now.ToLocalTime().ToString() + ")" + "Base64字符串:" + stringBase64 + "/n";
            Console.WriteLine(stringBase64);
            /** 图片转为base64 结束 */

            /** 发送http请求 开始 */
            this.textBox1.Text += "(" + DateTime.Now.ToLocalTime().ToString() + ")" + "发送http请求." + "\r\n";
            var postData = "file=" + stringBase64;
            var request = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:10086/userInfo/faceSearch");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            var data = Encoding.UTF8.GetBytes(postData.Replace("+", "%2B"));//转义
            request.ContentLength = data.Length;
            Stream newStream = request.GetRequestStream();
            newStream.Write(data, 0, data.Length);
            newStream.Close();
            WebResponse response = request.GetResponse();
            newStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(newStream);
            string responseFromServer = reader.ReadToEnd();
            this.textBox1.Text += "(" + DateTime.Now.ToLocalTime().ToString() + ")" + "请求结果：" + responseFromServer + "\r\n";
            reader.Close();
            response.Close();
            /** 发送http请求 结束 */

            /** json反序列化 开始 */
            JObject jo = (JObject)JsonConvert.DeserializeObject(responseFromServer);
            
            this.textBox1.Text += "(" + DateTime.Now.ToLocalTime().ToString() + ")" + "登录userid:"+ jo["data"]["userId"] + "\r\n";
            /** json反序列化 结束 */


        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisConnect();
        }
    }
}
