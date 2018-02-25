﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace MillionHerosHelper
{
    public partial class MainForm : Form
    {

        private ConfigForm configForm;
        private BrowserForm browserForm;
        private DaShangForm daShangForm;
        private Thread solveProblemThread;
        
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //便于并发请求同时进行
            ServicePointManager.MaxServicePoints = 128;
            ServicePointManager.DefaultConnectionLimit = 128;

            //禁用跨线程UI操作检查
            Control.CheckForIllegalCrossThreadCalls = false;

            Config.LoadConfig();

            BaiDuOCR.InitBaiDuOCR(Config.OCR_API_KEY, Config.OCR_SECRET_KEY);

            browserForm = new BrowserForm();
            browserForm.Show();
            MainForm_Move(null, null);

            //注册热键
            HotKey.RegisterHotKey(Handle, 100, HotKey.KeyModifiers.None, Keys.F7);
        }

        private void button_Config_Click(object sender, EventArgs e)
        {
            if (configForm == null || configForm.IsDisposed)
            {
                configForm = new ConfigForm();
                configForm.Show();
                configForm.Focus();
            }
            else
            {
                configForm.WindowState = FormWindowState.Normal;
                configForm.Focus();
            }
        }

        private void button_Pay_Click(object sender, EventArgs e)
        {
            if (daShangForm == null || daShangForm.IsDisposed)
            {
                daShangForm = new DaShangForm();
                daShangForm.Show();
                daShangForm.Focus();
            }
            else
            {
                daShangForm.WindowState = FormWindowState.Normal;
                daShangForm.Focus();
            }
        }


        private void MainForm_Move(object sender, EventArgs e)
        {
            if (browserForm != null && !browserForm.IsDisposed) 
            {
                browserForm.Location = new Point(this.Location.X + this.Width + 10, browserForm.Location.Y);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Config.SaveConfig();
            HotKey.UnregisterHotKey(Handle, 100);
        }

        private void linkLabel_Author_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/Azure99");
        }

        private void linkLabel_Fresh_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/Azure99/MillionHero/wiki/%E5%8A%A9%E6%89%8B%E5%9B%BE%E6%96%87%E4%BD%BF%E7%94%A8%E6%95%99%E7%A8%8B");
        }
        private void linkLabel_SourceCode_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/Azure99/MillionHerosHelper");
        }
        private void checkBox_InPutProblem_CheckedChanged(object sender, EventArgs e)
        {
            bool readOnly = !checkBox_InPutProblem.Checked;
            textBox_Problem.ReadOnly = readOnly;
            textBox_AnswerA.ReadOnly = readOnly;
            textBox_AnswerB.ReadOnly = readOnly;
            textBox_AnswerC.ReadOnly = readOnly;
            textBox_AnswerD.ReadOnly = readOnly;
        }

        private void linkLabel_DLNewVer_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/Azure99/MillionHero/releases");
        }

        private void button_SearchA_Click(object sender, EventArgs e)
        {
            string url;

            if (!Config.UseSoGou)
            {
                url = "http://www.baidu.com/s?wd=";
            }
            else
            {
                url = "https://www.sogou.com/web?query=";
            }

            if (Config.RemoveUselessInfo)//移除无用信息
            {
                browserForm.Jump(url + AnalyzeProblem.RemoveUselessInfo(textBox_Problem.Text + " " + textBox_AnswerA.Text));
            }
            else
            {
                browserForm.Jump(url + SearchEngine.UrlEncode(textBox_Problem.Text + " " + textBox_AnswerA.Text));
            }
        }

        private void button_SearchB_Click(object sender, EventArgs e)
        {
            string url;

            if (!Config.UseSoGou)
            {
                url = "http://www.baidu.com/s?wd=";
            }
            else
            {
                url = "https://www.sogou.com/web?query=";
            }

            if (Config.RemoveUselessInfo)//移除无用信息
            {
                browserForm.Jump(url + AnalyzeProblem.RemoveUselessInfo(textBox_Problem.Text + " " + textBox_AnswerB.Text));
            }
            else
            {
                browserForm.Jump(url + SearchEngine.UrlEncode(textBox_Problem.Text + " " + textBox_AnswerB.Text));
            }
        }

        private void button_SearchC_Click(object sender, EventArgs e)
        {
            string url;

            if (!Config.UseSoGou)
            {
                url = "http://www.baidu.com/s?wd=";
            }
            else
            {
                url = "https://www.sogou.com/web?query=";
            }


            if (Config.RemoveUselessInfo)//移除无用信息
            {
                browserForm.Jump(url + AnalyzeProblem.RemoveUselessInfo(textBox_Problem.Text + " " + textBox_AnswerC.Text));
            }
            else
            {
                browserForm.Jump(url + SearchEngine.UrlEncode(textBox_Problem.Text + " " + textBox_AnswerC.Text));
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                if (m.WParam.ToInt32() == 100) 
                {
                    if(button_Start.Enabled)
                    {
                        button_Start_Click(null, null);
                    }
                }
            }

            base.WndProc(ref m);
        }


        private void button_Start_Click(object sender, EventArgs e)
        {
            button_Config.Enabled = false;
            button_Start.Enabled = false;

            label_AnalyzeA.Text = "";
            label_AnalyzeB.Text = "";
            label_AnalyzeC.Text = "";
            label_AnalyzeD.Text = "";

            solveProblemThread = new Thread(new ThreadStart(BeginSolveProblem));
            solveProblemThread.Start();

            int timeUsed = 0;
            System.Timers.Timer monitor = new System.Timers.Timer(100);//答题时间监视
            monitor.Elapsed += (object _sender, System.Timers.ElapsedEventArgs _args) =>
            {
                if (solveProblemThread == null)
                {
                    monitor.Stop();
                    monitor.Close();
                }
                else if (timeUsed > 22000)
                {
                    //solveProblemThread.Abort();
                    FinishSolveProblem();
                    label_Message.Text = "题目分析超时";
                    label_Message.ForeColor = Color.Red;
                    MessageBox.Show("答题过程超过22秒,自动终止.\r\n请确保您的网络环境良好!", "执行超时", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    timeUsed += 100;
                }
            };
            monitor.Start();
        }

        #region 答题部分
        private void BeginSolveProblem()
        {
            try
            {
                SolveProblem();
            }
            catch (ADBException ex)
            {
                label_Message.Text = "手机连接出错";
                label_Message.ForeColor = Color.Red;
                MessageBox.Show("请确保已连接手机并配置正确" + "\r\n\r\n详情:\r\n" + ex.ToString(), "ADB手机连接错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (OCRException ex)
            {
                label_Message.Text = "题目识别出错";
                label_Message.ForeColor = Color.Red;
                MessageBox.Show("请确保手机在题目界面" + "\r\n\r\n详情:\r\n" + ex.ToString(), "文本识别错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (APIException ex)
            {
                label_Message.Text = "网络连接出错";
                label_Message.ForeColor = Color.Red;
                MessageBox.Show("请确保网络连接正常以及API可用" + "\r\n\r\n详情:\r\n" + ex.ToString(), "API错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IndexOutOfRangeException ex)
            {
                label_Message.Text = "题目识别出错";
                label_Message.ForeColor = Color.Red;
                MessageBox.Show("请确保手机在题目界面" + "\r\n\r\n详情:\r\n" + ex.ToString(), "解析错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (WebException ex)
            {
                label_Message.Text = "网络连接出错";
                label_Message.ForeColor = Color.Red;
                MessageBox.Show("请确保网络环境良好" + "\r\n\r\n详情:\r\n" + ex.ToString(), "网络错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                label_Message.Text = "未知错误";
                label_Message.ForeColor = Color.Red;
                MessageBox.Show(ex.ToString(), "未知错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                FinishSolveProblem();
            }
        }
        private void FinishSolveProblem()
        {
            button_Config.Enabled = true;
            button_Start.Enabled = true;
            solveProblemThread = null;
        }
        private void SolveProblem()//答题
        {
            if (!checkBox_InPutProblem.Checked)
            {
                label_Message.Text = "正在获取手机界面";
                label_Message.ForeColor = Color.Orange;
                //获取屏幕截图
                string screenShotPath;
                byte[] smallScreenShot;
                try
                {
                    if (Config.UseEmulator)//是否为模拟器
                    {
                        smallScreenShot = BitmapOperation.CutScreen(new Point(Config.CutX, Config.CutY), new Size(Config.CutWidth, Config.CutHeight));
                    }
                    else
                    {
                        screenShotPath = ADB.GetScreenshotPath();
                        smallScreenShot = BitmapOperation.CutImage(screenShotPath, new Point(Config.CutX, Config.CutY), new Size(Config.CutWidth, Config.CutHeight));
                        System.IO.File.Delete(screenShotPath);
                    }
                }
                catch (Exception ex)
                {
                    throw new ADBException("获取的屏幕截图无效!" + ex);
                }
                label_Message.Text = "正在识别题目信息";
                //调用API识别文字
                string recognizeResult = BaiDuOCR.Recognize(smallScreenShot);

                string[] recRes = Regex.Split(recognizeResult, "\r\n|\r|\n");
                //检查识别结果正确性
                CheckOCRResult(recRes);
                //显示识别结果
                int notEmptyIndex = recRes.Length - 1;
                while (String.IsNullOrEmpty(recRes[notEmptyIndex]))//忽略空行
                {
                    notEmptyIndex--;
                }

                if (checkBox_EnableD.Checked) 
                {
                    textBox_AnswerD.Text = AnalyzeProblem.RemoveABC(recRes[notEmptyIndex--]);
                }

                textBox_AnswerC.Text = AnalyzeProblem.RemoveABC(recRes[notEmptyIndex--]);
                textBox_AnswerB.Text = AnalyzeProblem.RemoveABC(recRes[notEmptyIndex--]);
                textBox_AnswerA.Text = AnalyzeProblem.RemoveABC(recRes[notEmptyIndex--]);

                string problem = recRes[0];

                int dotP = problem.IndexOf('.');
                if (dotP != -1)
                {
                    problem = problem.Substring(dotP + 1, problem.Length - dotP - 1);
                }

                for (int i = 1; i <= notEmptyIndex; i++)
                {
                    problem += recRes[i];
                }

                textBox_Problem.Text = problem;
            }

            string url;

            if (!Config.UseSoGou)
            {
                url = "http://www.baidu.com/s?wd=";
            }
            else
            {
                url = "https://www.sogou.com/web?query=";
            }

            if (Config.RemoveUselessInfo) 
            {
                url += SearchEngine.UrlEncode(AnalyzeProblem.RemoveUselessInfo(textBox_Problem.Text));
            }
            else
            {
                url += SearchEngine.UrlEncode(textBox_Problem.Text);
            }

            string[] answerArr;
            if (checkBox_EnableD.Checked)
            {
                answerArr = new string[] { textBox_AnswerA.Text, textBox_AnswerB.Text, textBox_AnswerC.Text , textBox_AnswerD.Text};
            }
            else
            {
                answerArr = new string[] { textBox_AnswerA.Text, textBox_AnswerB.Text, textBox_AnswerC.Text };
            }

            browserForm.HighlightAndShowPage(url, answerArr);
            browserForm.Show();
            browserForm.WindowState = FormWindowState.Normal;

            label_Message.Text = "正在分析题目";
            //分析问题
            AnalyzeResult aRes = AnalyzeProblem.Analyze(textBox_Problem.Text, answerArr);
            char[] ans = new char[4] { 'A', 'B', 'C' , 'D'};
            label_Message.Text = "最有可能选择:" + ans[aRes.Index] + "项!" + answerArr[aRes.Index];
            if (aRes.Oppose)
            {
                label_Message.Text += "(包含否定词)";
            }

            label_Message.ForeColor = Color.Green;
            label_AnalyzeA.ForeColor = Color.DarkGreen;
            label_AnalyzeB.ForeColor = Color.DarkGreen;
            label_AnalyzeC.ForeColor = Color.DarkGreen;
            label_AnalyzeD.ForeColor = Color.DarkGreen;
            textBox_AnswerA.ForeColor = Color.Black;
            textBox_AnswerB.ForeColor = Color.Black;
            textBox_AnswerC.ForeColor = Color.Black;
            textBox_AnswerD.ForeColor = Color.Black;

            switch (aRes.Index)
            {
                case 0: label_AnalyzeA.ForeColor = Color.Red;
                        textBox_AnswerA.ForeColor = Color.Red;
                        break;
                case 1: label_AnalyzeB.ForeColor = Color.Red; 
                        textBox_AnswerB.ForeColor = Color.Red;
                        break;
                case 2: label_AnalyzeC.ForeColor = Color.Red; 
                        textBox_AnswerC.ForeColor = Color.Red;
                        break;
                case 3: label_AnalyzeD.ForeColor = Color.Red;
                        textBox_AnswerD.ForeColor = Color.Red;
                        break;
            }

            //显示概率
            label_AnalyzeA.Text = "概率:" + aRes.Probability[0].ToString() + "%";
            label_AnalyzeB.Text = "概率:" + aRes.Probability[1].ToString() + "%";
            label_AnalyzeC.Text = "概率:" + aRes.Probability[2].ToString() + "%";
            if(checkBox_EnableD.Checked)
            {
                label_AnalyzeD.Text = "概率:" + aRes.Probability[3].ToString() + "%"; 
            }
        }

        /// <summary>
        /// 检查OCR结果是否合法
        /// </summary>
        /// <param name="arr"></param>
        private void CheckOCRResult(string[] arr)
        {
            if (arr.Length > 10)
            {
                throw new OCRException("识别到的文本过多");
            }
            else if (arr.Length > 0 && arr.Length < 4)
            {
                throw new OCRException("识别到的文本过少");
            }
            else if (arr.Length == 0)
            {
                throw new OCRException("没有识别到文本");
            }
        }
        #endregion

        private void checkBox_EnableD_CheckStateChanged(object sender, EventArgs e)
        {
            bool ckd = checkBox_EnableD.Checked;
            label_AnswerD.Enabled = ckd;
            label_AnalyzeD.Enabled = ckd;
            button_SearchD.Enabled = ckd;
            textBox_AnswerD.Enabled = ckd;
        }
    }
}
