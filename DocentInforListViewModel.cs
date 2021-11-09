using netcore.smtp.lib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using TechfairsSmtpSender.Entity;
using TechfairsSmtpSender.Environment;
using TechfairsSmtpSender.Model;
using TechfairsSmtpSender.View;

namespace TechfairsSmtpSender.ViewModel
{
    public class DocentInforListViewModel : BaseViewModel
    {
        private DocentInformation _selectList;

        public ICommand MailSendCommandButton { get; set; }
        public ObservableCollection<DocentInformation> DocentInfoList { get; set; }
        public DocentInformation SelectList 
        {
            get => _selectList;
            set
            {
                _selectList = value;

                if (TSSManager.Instance.IsCheckLoad)
                {
                    ReadRawDataView rawDataView = (ReadRawDataView)TSSManager.Instance.GetUserControl("Main_ReadRawDataView");
                    if (rawDataView == null || _selectList.RawDocentInformation.author == null)
                        return;

                    string jsonString = JsonConvert.SerializeObject(_selectList);
                    jsonString = TSSManager.Instance.Beautify(jsonString);

                    rawDataView.rawGrid.Children.Clear();

                    TextBlock tb = new TextBlock();
                    tb.Text = jsonString;
                    rawDataView.rawGrid.Children.Add(tb);
                }

            }
        }

        //-------------------------------------------------------- Constructor
        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="count"></param>
        public DocentInforListViewModel(int count)
        {
            DocentInfoList = new ObservableCollection<DocentInformation>();
            SelectList = new DocentInformation();
            MailSendCommandButton = new RelayCommand(MailSendCommand);

            // Clear
            if (count == 0)
                return;

            int endNum = count * 20;
            int startNum = endNum - 20;

            int inforCount = TSSManager.Instance.GetAllInformation().DocentInformations.Count;
            if (endNum > inforCount)
                endNum = inforCount;

            DocentInfoList.Clear();

            List<DocentInformation> docents = TSSManager.Instance.GetAllInformation().DocentInformations;

            for (int i = startNum; i < endNum; i++)
            {
                InitData(i, docents);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="docents">Model Data</param>
        /// <param name="index"></param>
        public DocentInforListViewModel(List<DocentInformation> docents, int index = 0)
        {
            DocentInfoList = new ObservableCollection<DocentInformation>();
            SelectList = new DocentInformation();
            MailSendCommandButton = new RelayCommand(MailSendCommand);

            int endNum = 0;
            int startNum = 0;

            if (index != 0)
            {
                // 1 page
                if (docents.Count < 20)
                {
                    endNum = docents.Count;
                    startNum = 0;
                }
                else if (docents.Count < index * 20)
                {
                    endNum = docents.Count;
                    startNum = (index - 1) * 20;
                }
                // 1 page 이상
                else
                {
                    endNum = index * 20;
                    startNum = endNum - 20;
                }

                for (int i = startNum; i < endNum; i++)
                {
                    InitData(i, docents);
                }
            }
            else
            {
                // 1 page
                if (docents.Count < 20)
                {
                    endNum = docents.Count;
                    startNum = 0;
                }
                // 1 page 이상
                else
                {
                    endNum = 20;
                    startNum = 0;
                }

                for (int i = startNum; i < endNum; i++)
                {
                    InitData(i, docents);
                }
            }
        }

        /// <summary>
        /// 1. 검색 입력 후 다음, 이전 페이지 적용시에 사용
        /// 2. startCount 값이 -1 이면, 이전 이벤트 작동
        /// </summary>
        /// <param name="index"></param>
        /// <param name="startNum"></param>
        /// <param name="pageRemaining"></param>
        public DocentInforListViewModel(int index, int startNum, int endNum)
        {
            index += 1;
            DocentInfoList = new ObservableCollection<DocentInformation>();
            SelectList = new DocentInformation();
            MailSendCommandButton = new RelayCommand(MailSendCommand);

            if (startNum == -1)
            {
                endNum = index * 20;
                startNum = endNum - 20;
            }

            List<DocentInformation> docents = TSSManager.Instance.SearchDocentInfors;
            for (int i = startNum; i < endNum; i++)
            {
                InitData(i, docents);
            }
        }
        #endregion

        /// <summary>
        /// MailSend Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="param"></param>
        /// <param name="targetName"></param>
        private void MailSendCommand(object sender, object param, string targetName)
        {
            DocentInformation docent = param as DocentInformation;

            if (docent == null)
                return;

            if (docent.Status.Color == System.Windows.Media.Colors.Red)
            {
                MessageBox.Show("Mail to 가 없어 메일을 보낼 수 없습니다.");
                return;
            }

            string image1 = TSSManager.Instance.ImagePath + @"\TechFair.png";
            string image2 = TSSManager.Instance.ImagePath + @"\WarningMessage.gif";

            if (docent.Type == "bookmark")
            {
                if (docent.RawDocentInformation.data == null)
                {
                    MessageBox.Show("bookmark : 잘못된 형식의 파일입니다. 다른 파일을 불러 오십시오.");
                    return;
                }
                
                string htmlString = TSSManager.Instance.ParsingHTML(docent.RawDocentInformation);
                var result = IMSmtp.SendSMTPBookmark(docent.MailTo, docent.RawDocentInformation.title, htmlString, image1, image2);

                if (result == "OK")
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        docent.Status.Color = Colors.Green;
                        docent.TimeSentMail = DateTime.Now.ToString();
                    });
                    TSSManager.Instance.AddMD5(docent.MD5);
                    TSSManager.Instance.AddLog(docent.Author, docent.MailTo, docent.MD5, docent.TimeSentMail);
                    TSSManager.Instance.WriteToCheckLogDB(new LogDB()
                    {
                        Author = docent.Author,
                        MailTo = docent.MailTo,
                        MD5 = docent.MD5,
                        TimeSentMail = docent.TimeSentMail
                    });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        docent.Status.Color = Colors.Red;
                    });
                }
            }
            else
            {
                if (docent.InquiryInformation.data == null)
                {
                    MessageBox.Show("bookmark : 잘못된 형식의 파일입니다. 다른 파일을 불러 오십시오.");
                    return;
                }

                string htmlString = TSSManager.Instance.ParsingInquiryHTML(docent.InquiryInformation);
                var result = IMSmtp.SendSMTPInquiry(docent.MailTo, docent.InquiryInformation.title, htmlString, image1, image2);

                if (result == "OK")
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        docent.Status.Color = Colors.Green;
                        docent.TimeSentMail = DateTime.Now.ToString();
                        TSSManager.Instance.AddMD5(docent.MD5);
                        TSSManager.Instance.AddLog(docent.Author, docent.MailTo, docent.MD5, docent.TimeSentMail);
                        TSSManager.Instance.WriteToCheckLogDB(new LogDB()
                        {
                            Author = docent.Author,
                            MailTo = docent.MailTo,
                            MD5 = docent.MD5,
                            TimeSentMail = docent.TimeSentMail
                        });
                    });
                    
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        docent.Status.Color = Colors.Red;
                    });
                }
            }
        }

        /// <summary>
        /// 초기화 함수
        /// </summary>
        /// <param name="i"></param>
        /// <param name="docents">Model Data</param>
        private void InitData(int i, List<DocentInformation> docents)
        {
            DocentInfoList.Add(docents[i]);
            int listCount = DocentInfoList.Count - 1;

            if (docents[i].Status == null)
                DocentInfoList[listCount].Status = new SolidColorBrush(Colors.Red);
            else
                DocentInfoList[listCount].Status = docents[i].Status;
            DocentInfoList[listCount].Index = i + 1;

            if (docents[i].Type == "inquiry")
            {
                DocentInfoList[listCount].Author = docents[i].InquiryInformation.author;
                DocentInfoList[listCount].Type = docents[i].InquiryInformation.type;
                DocentInfoList[listCount].TypeColor = new SolidColorBrush(Colors.DarkGreen);

                DocentInfoList[listCount].MailFrom = docents[i].InquiryInformation.mailfrom;
                DocentInfoList[listCount].MailTo = docents[i].InquiryInformation.mailto;
                DocentInfoList[listCount].Date = docents[i].InquiryInformation.createAt;
            }
            else
            {

                DocentInfoList[listCount].Author = docents[i].RawDocentInformation.author;
                DocentInfoList[listCount].Type = docents[i].RawDocentInformation.type;
                DocentInfoList[listCount].TypeColor = new SolidColorBrush(Colors.Blue);

                DocentInfoList[listCount].MailFrom = docents[i].RawDocentInformation.mailfrom;
                DocentInfoList[listCount].MailTo = docents[i].RawDocentInformation.mailto;
                DocentInfoList[listCount].Date = docents[i].RawDocentInformation.createAt;
            }
        }

    }
}
