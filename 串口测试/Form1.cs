using System; 
using System.Collections.Generic;
using System.ComponentModel; 
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text; 
using System.Windows.Forms;
using System.IO.Ports;
using Microsoft.VisualBasic;
using System.Timers;
using System.Threading;
using System.IO;


namespace 串口测试
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();

        }
        private SerialPort mySerialPort = new SerialPort();
        private StringBuilder builder = new StringBuilder();//避免在事件处理方法中反复的创建，定义到外面。   
        private void Form1_Load(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            comboPortName.Items.AddRange(ports);
            comboPortName.SelectedIndex = comboPortName.Items.Count > 0 ? 0 : -1;
            comboBaudrate.SelectedIndex = comboBaudrate.Items.IndexOf("9600");
            mySerialPort.StopBits = StopBits.One;
            //初始化SerialPort对象   
            mySerialPort.NewLine = "\r\n";
            mySerialPort.RtsEnable = true;
            //采用ASCII编码方式
            mySerialPort.Encoding = Encoding.ASCII;
            //接收到一个字符就触发接收事件
            mySerialPort.ReceivedBytesThreshold = 1;
            

        }
        
        
      








        //校验：
        public static void CalculateCRC(byte[] pByte, int nNumberOfBytes, out ushort pChecksum)
        {
            int nBit;
            ushort nShiftedBit;
            pChecksum = 0xFFFF;

            for (int nByte = 0; nByte < nNumberOfBytes; nByte++)
            {
                pChecksum ^= pByte[nByte];
                for (nBit = 0; nBit < 8; nBit++)
                {
                    if ((pChecksum & 0x1) == 1)
                    {
                        nShiftedBit = 1;
                    }
                    else
                    {
                        nShiftedBit = 0;
                    }
                    pChecksum >>= 1;
                    if (nShiftedBit != 0)
                    {
                        pChecksum ^= 0xA001;
                    }
                }
            }
        }
        //校验：
        public static void CalculateCRC(byte[] pByte, int nNumberOfBytes, out byte hi, out byte lo)
        {
            ushort sum;
            CalculateCRC(pByte, nNumberOfBytes, out sum);
            lo = (byte)(sum & 0xFF);
            hi = (byte)((sum & 0xFF00) >> 8);
        }

        //打包方法，可以将字符串转成byte[] 
        public byte[] mysendb(string s)
        {
            string temps = delspace(s);

            if (temps.Length % 2 != 0)
            {
                temps = "0" + temps;
            }

            byte[] tempb = new byte[50];
            int j = 0;

            for (int i = 0; i < temps.Length; i = i + 2, j++)
            {
                tempb[j] = Convert.ToByte(temps.Substring(i, 2), 16);
            }

            byte[] send = new byte[j];
            Array.Copy(tempb, send, j);
            return send;
        }
        //除去空格
        public string delspace(string putin)
        {
            string putout = "";

            for (int i = 0; i < putin.Length; i++)
            {
                if (putin[i] != ' ')
                    putout += putin[i];
            }

            return putout;
        }
        //数据转换
        public static void formatstring(string strinput, int length, out string stroutput, out Boolean Valid)
        {
            stroutput = "";
            Valid = true;
            byte temp;

            if ((strinput.Length <= length) & (strinput.Length > 0))
            {
                for (int i = 0; i < strinput.Length; i++)
                {
                    try
                    {
                        temp = Convert.ToByte(strinput[i].ToString(), 16);
                    }

                    catch
                    {
                        Valid = false;
                        stroutput = "";
                        break;
                    }

                    stroutput += strinput[i];
                }

                if (Valid & (strinput.Length < length))
                {
                    for (int j = 0; j < length - strinput.Length; j++)
                    {
                        stroutput = "0" + stroutput;
                    }
                }
            }

            else
            {
                Valid = false;
                stroutput = "";
            }
        }
        //发送数据
        private void btnsend_Click()
        {
            //btn_sender = ((Button)sender).Text.ToString();
            byte[] defByte = new byte[6];
            //接收
            txtRcv1.Text = "";

            //设备号         
            string str1x = textBox3.Text.ToString(); ;
            string str1 = "";
            Boolean Macvalid1;

            formatstring(str1x, 2, out str1, out Macvalid1);

            if (!Macvalid1)
            {
                MessageBox.Show("设备地址数值不符合规范，最多输入2位十六进制数", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;

            }

            byte[] numbyte1 = this.mysendb(str1);
            defByte[0] = numbyte1[0];

            //功能码 - 04

            string fun_str1 = textBox4.Text.ToString() ;
            byte[] fun_numbyte1 = this.mysendb(fun_str1);
            defByte[1] = fun_numbyte1[0];

            //寄存器地址
            string str2x = textBox1.Text.ToString();
            string str2 = "";
            Boolean addrvalid1;

            formatstring(str2x, 4, out str2, out addrvalid1);

            //if (!addrvalid1)
            //{
            //    MessageBox.Show("寄存器地址数值不符合规范，最多输入4位十六进制数", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}

            // textBoxInformation.AppendText(str2);

            byte[] numbyte2 = this.mysendb(str2);
            defByte[2] = numbyte2[0];
            defByte[3] = numbyte2[1];

            //写入的数据
            string str3x =textBox2.Text.ToString();
            string str3 = "";
            Boolean valuevalid1;

            formatstring(str3x, 4, out str3, out valuevalid1);

            //if (!valuevalid1)
            //{
            //    MessageBox.Show("输入数值不符合规范，最多输入4位十六进制数", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}

            byte[] numbyte3 = this.mysendb(str3);
            defByte[4] = numbyte3[0];
            defByte[5] = numbyte3[1];

            //计算CRC
            byte crch = 0;
            byte crcl = 0;

            CalculateCRC(defByte, defByte.Length, out crch, out crcl);

            // MOVE给新的数组
            byte[] rebyte = new byte[defByte.Length + 2];

            for (int i = 0; i < defByte.Length; i++)
            {
                rebyte[i] = defByte[i];
            }

            rebyte[6] = crcl;
            rebyte[7] = crch;

            //received = false;
            this.mySerialPort.Write(rebyte, 0, rebyte.Length); // 发送 
            
            //起动计时器
            timer1.Enabled = true;

            //textBoxInformation.AppendText("发送成功\r\n");


            //将发送数据显示在TEXTBOX中
            txtsend1.Text = "";

            string strSend = "";

            for (int i = 0; i < rebyte.Length; i++)
            {
                strSend += string.Format("{0:X2} ", rebyte[i]);
            }

            this.txtsend1.Text = strSend;
            strSend = null;   
        }
        int panduan = 0;
        private void button1_Click(object sender, EventArgs e)
        {
            if(mySerialPort.IsOpen)
            {
                MessageBox.Show("端口已打开");
            }

            else
            {
                //设置端口号
                mySerialPort.PortName = comboPortName.SelectedItem.ToString();
                //设置波特率
                mySerialPort.BaudRate = Convert.ToInt32(comboBaudrate.SelectedItem);
                //数据位
                mySerialPort.DataBits = 8;
                //校验位
                mySerialPort.Parity = Parity.None;
                //停止位
                mySerialPort.StopBits = StopBits.One;
                //采用ASCII编码方式
                mySerialPort.Encoding = Encoding.ASCII;
                //接收到一个字符就触发接收事件
                mySerialPort.ReceivedBytesThreshold = 1;
                mySerialPort.Open();            //打开串口
                MessageBox.Show(comboPortName.Text + "已经打开");

            }
           
            
            
        }
        int i = 1;//发送循环标志
        int i1 = 0;//"已点击发送" 标志
        private void button2_Click(object sender, EventArgs e)
        {
            if (i1 == 0)
            {
                Thread t = new Thread(WriteY);//开启新进程
                t.Start();
                void WriteY()
                {
                    Form.CheckForIllegalCrossThreadCalls = false;
                    i = 1;
                    while (i == 1)
                    {
                        //给串口缓存写入初始数据
                        if (panduan == 0)
                        {

                            if (mySerialPort.IsOpen)
                            {
                                btnsend_Click();
                                byte[] b = new byte[mySerialPort.BytesToRead];//定义byte数组,serialPort读取数据               

                                mySerialPort.Read(b, 0, b.Length);

                                string str = "";
                                for (int i = 0; i < b.Length; i++)
                                {
                                    str += string.Format("{0:X2} ", b[i]);
                                }
                                //MessageBox.Show("预处理完毕，请再点击发送");
                                panduan = 1;

                            }
                            else
                            {
                                MessageBox.Show("串口未打开");
                                return;
                            }
                        }


                        else
                        {


                            if (mySerialPort.IsOpen)//判断端口是否开启
                            {


                                btnsend_Click();
                                byte[] b = new byte[mySerialPort.BytesToRead];//定义byte数组,serialPort读取数据               

                                mySerialPort.Read(b, 0, b.Length);

                                string str = "";
                                for (int i = 0; i < b.Length; i++)
                                {
                                    str += string.Format("{0:X2} ", b[i]);
                                }
                                //str = "0B 05 11 11 21 12";//测试用 给初始数据
                                richTextBox1.Text = str;
                                str = delspace(str);//去空格



                                if ("" == str)//判断是否有数据
                                {
                                    MessageBox.Show("无数据");
                                    break;
                                }
                                else
                                {
                                    string a;//定义湿度变量a
                                    str = str.Remove(0, 34);  //去掉前6个字符
                                    a = str;
                                    a = a.Remove(0, 4);//去掉4位温度
                                    a = a.Substring(0, 4);//取前4个字符
                                    str = str.Substring(0, 4);//取前4个字符



                                    a = Convert.ToString(Convert.ToInt32(a, 16) * 0.1);
                                    str = Convert.ToString(Convert.ToInt32(str, 16) * 0.1);

                                    txtRcv1.Text = str;
                                    richTextBox2.Text = a;
                              



                                    //创建一个文本文件,先判断一下 
                                    StreamWriter sw;
                                    if (!File.Exists("data.txt"))
                                    {
                                        //不存在就新建一个文本文件,并写入一些内容 
                                        sw = File.CreateText("data.txt");

                                        sw.WriteLine(" 数据采集程序");

                                        sw.WriteLine("{0}={1}", DateTime.Now, str);
                                        sw.Close();
                                    }
                                    else
                                    {
                                        //如果存在就添加一些文本内容 
                                        sw = File.AppendText("data.txt");
                                        sw.WriteLine("{0}={1}", DateTime.Now, str);
                                        sw.Close();

                                    }



                                }


                            }
                            else
                            {
                                MessageBox.Show("串口未打开");
                                return;
                            }



                        }
                        if (i == 0)
                        {
                            break;
                        }
                        int cy = Convert.ToInt32(textBox5.Text);
                        Thread.Sleep(cy);
                    }
                }

                i1 = 1;
            }
            else
            {   
                MessageBox.Show("正在发送中");


            }

          
        }
        

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
             i = 0;
            i1 = 0;
        }

        private void txtRcv1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
           
                
        }



        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            /// <summary>
            /// 添加双击托盘图标事件（双击显示窗口）
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
          
                if (WindowState == FormWindowState.Minimized)
                {
                    //还原窗体显示    
                    WindowState = FormWindowState.Normal;
                    //激活窗体并给予它焦点
                    this.Activate();
                    //任务栏区显示图标
                    this.ShowInTaskbar = true;
                    //托盘区图标隐藏
                    notifyIcon1.Visible = false;
                }
         }

        private void F_Main_SizeChanged(object sender, EventArgs e)
        {
            //判断是否选择的是最小化按钮
            if (WindowState == FormWindowState.Minimized)
            {
                //隐藏任务栏区图标
                this.ShowInTaskbar = false;
                //图标显示在托盘区
                notifyIcon1.Visible = true;
            }
        }
        /// <summary>
        /// 确认是否退出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void F_Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("是否确认退出程序？", "退出", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                // 关闭所有的线程
                i = 0;//关闭前停止循环
                this.Dispose();
                this.Close();
            }
            else
            {
                e.Cancel = true;
            }
            
        }

        /// <summary>
        /// 托盘右键显示主界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 显示ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
        }
        /// <summary>
        /// 托盘右键退出程序
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否确认退出程序？", "退出", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                // 关闭所有的线程
                this.Dispose();
                this.Close();
            }
        }

        private void label2_Click_1(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        //private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    byte[] b = new byte[mySerialPort.BytesToRead];//定义byte数组,serialPort读取数据               

        //    mySerialPort.Read(b, 0, b.Length);

        //    string str = "";
        //    for (int i = 0; i < b.Length; i++)
        //    {
        //        str += string.Format("{0:X2} ", b[i]);
        //    }
        //    txtRcv1.Text= str;
        //}
    }
}
