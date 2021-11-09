using netcore.smtp.lib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TechfairsSmtpSender.Entity;
using TechfairsSmtpSender.Model;

namespace TechfairsSmtpSender.View
{
    /// <summary>
    /// SearchView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SearchView : UserControl
    {
        private int _standardPage = 1;

        public SearchView()
        {
            InitializeComponent();
            this.Loaded += SearchView_Loaded;
        }

        private void SearchView_Loaded(object sender, RoutedEventArgs e)
        {
            this.SearchComboBox.Items.Add("Author");
            this.SearchComboBox.Items.Add("Mail");
            this.SearchComboBox.Items.Add("Date");

            this.SearchComboBox.SelectionChanged += SearchComboBox_SelectionChanged;
            this.SearchTextBox.TextChanged += SearchTextBox_TextChanged;

            this.SearchPickDateView.FirstDate.SelectedDateChanged += FirstDate_SelectedDateChanged;
            this.SearchPickDateView.SecondDate.SelectedDateChanged += SecondDate_SelectedDateChanged;

            this.SendAllButton.Click += SendAllButton_Click;

            this.SendAllButton.IsEnabled = false;
        }


        //-------------------------------------------------------- Send All Email
        #region Send All Email

        private void SendAllButton_Click(object sender, RoutedEventArgs e)
        {
            isTaskCheck = true;
            TSSManager.Instance.RemoveDuplicateLogDB();
            SemaphoreSlim(TSSManager.Instance.GetAllInformation().DocentInformations);
        }

        int checkCount = 0; // 중복메일 체크

        private async void SemaphoreSlim(List<Model.DocentInformation> docents)
        {

            ProgressBarWindow pb = new ProgressBarWindow();
            pb.Width = 300;
            pb.Height = 120;
            pb.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
            pb.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            //pb.Topmost = true;
            pb.Show();

            DocentInforListView diView =  (DocentInforListView)TSSManager.Instance.GetUserControl("DocentView");
            diView.IsEnabled = false;

            smCount = 0;        // Send Email Count
            checkCount = 0;     // Check Send Mail

            int maxCount = 7;
            int count = docents.Count / maxCount;
            int endCount = docents.Count % maxCount;

            DateTime fdt = DateTime.Now;

            // 필터
            DateTime previousTime = DateTime.Now.AddMinutes(-1 * CONFIG.int_standard_time_min);
            List<LogDB> filterLogs = TSSManager.Instance.LogDBs.FindAll(x => DateTime.Compare(DateTime.Parse(x.TimeSentMail), previousTime) > 0);


            // Data Count
            int datCount = 0;
            var documents = TSSManager.Instance.GetAllInformation().DocentInformations;
            for (int i = 0; i < documents.Count; i++)
            {
                if (documents[i].TimeSentMail == null && documents[i].Status.Color != Colors.Red)
                    datCount++;
            }

            #region 소스

            // 나머지가 있을경우는 + 1
            if (endCount != 0)
            {
                count += 1;
                for (int i = 0; i < count; i++)
                {
                    if (!isTaskCheck)
                        return;

                    if (i == count - 1)
                    {
                        //await Task.Run(async () =>
                        //{
                        //    var result = await Function(i * maxCount, i * maxCount + endCount, docents, filterLogs, datCount);
                        //    isTaskCheck = result;

                        //    if (!result)
                        //        return;
                        //});

                        var result = await Task.Run(async () =>
                        {
                            return await Function(i * maxCount, i * maxCount + endCount, docents, filterLogs, datCount);
                        });

                        if (!result)
                            break;
                    }
                    else
                    {
                        //await Task.Run(async () =>
                        //{
                        //    var result = await Function(i * maxCount, i * maxCount + maxCount, docents, filterLogs, datCount);
                        //    isTaskCheck = result;

                        //    if (!result)
                        //        return;
                        //});

                        var result = await Task.Run(async () =>
                        {
                            return await Function(i * maxCount, i * maxCount + maxCount, docents, filterLogs, datCount);
                        });

                        if (!result)
                            break;

                    }
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    if (!isTaskCheck)
                        return;

                    checkCount++;

                    //await Task.Run(async () =>
                    //{
                    //    var result = await Function(i * maxCount, i * maxCount + maxCount, docents, filterLogs, datCount);
                    //    isTaskCheck = result;
                    //    if (!result)
                    //        return;
                    //});

                    var result = await Task.Run(async () =>
                    {
                        return await Function(i * maxCount, i * maxCount + maxCount, docents, filterLogs, datCount);
                    });
                    isTaskCheck = result;
                    if (!result)
                        break;
                }
            }
            DateTime edt = DateTime.Now;
            var rdt = edt - fdt;
            #endregion


            //await Task.Run(async() =>
            //{
            //    for (int i = 0; i < 5; i++)
            //    {
            //        await Task.Delay(1000);
            //    }
            //});

            pb.Close();
            diView.IsEnabled = true;


            //MD5 중복 제거
            TSSManager.Instance.RemoveDuplicateMD5();
            TSSManager.Instance.RemoveDuplicateLogDB();

        }

        private bool isTaskCheck { get; set; } = true;



        private List<Model.DocentInformation> listDocent = null;
        private List<string> listError = null;
        private int smCount = 0;


        public async Task<bool> Function(int start, int end, List<Model.DocentInformation> docents, List<LogDB> filterLogs, int datCount)
        {
            listDocent = new List<Model.DocentInformation>();
            listError = new List<string>();

            for (int i = start; i < end; i++)
            {
                checkCount++;

                // Check Mail to
                var isColor = this.Dispatcher.Invoke(() =>
                {
                    bool isColor = false;
                    if (docents[i].Status.Color == System.Windows.Media.Colors.Red)
                        isColor = true;

                    return isColor;
                });

                if (isColor)
                    continue;


                // 중복 체크
                if (TSSManager.Instance.InspectMD5(docents[i].MD5))
                    continue;

                var result = await FindPreviousTimeData(smCount, docents[i], filterLogs, datCount);

                if (!result)
                    return result;

                smCount++;
            }

            // 모든 페이지가
            //if (checkCount == TSSManager.Instance.GetAllInformation().DocentInformations.Count)
            //    MessageBox.Show("중복된 메일은 전달하지 않습니다.");

            return true;
        }

        private async Task<bool> FindPreviousTimeData(int sendMailCount, DocentInformation docent, List<LogDB> filterLogs, int datCount)
        {
            // ex) 10분전으로 적용하기 위해서 -1을 곱한다.
            int standardTime = CONFIG.int_standard_time_min;
            int standardCount = CONFIG.int_standard_count;
            int filterLogsCount = filterLogs.Count;


            // 메일을 보낼 수 없습니다.
            // ~~ 분 후에 다시 보내 십시오.
            if (filterLogsCount >= standardCount || standardCount - filterLogsCount - 1 < sendMailCount)
            {

                if (filterLogsCount == 0)
                {
                    MessageBox.Show($"메일을 보낼 수 없습니다. {DateTime.Now.AddMinutes(10)} 이 후에 보내십시오.");
                    return false;
                }
                else
                {
                    int index = filterLogsCount - 1;
                    DateTime listSendMail = DateTime.Parse(filterLogs[index].TimeSentMail);
                    DateTime nextTimeSendMail = listSendMail.AddMinutes(standardTime);
                    MessageBox.Show($"메일을 보낼 수 없습니다. {nextTimeSendMail} 이 후에 보내십시오.");
                    return false;
                }

              
            }
            // 보내는 메일 수와, 데이터 수가 같을 경우
            else if (sendMailCount == datCount - 1  )
            {
                SendAllMailFunction(docent);
                
                // 모든 메일을 보냈습니다. 문구
                MessageBox.Show($"모든 메일을 보냈습니다.");
                return false;
            }
            else
            {
                // 사용 할 수 있는 수
                int useCount = standardCount - filterLogsCount;

                // 1. DB가 useCount보다 작아서 메일을 모두 보낸 경우
                if (sendMailCount < useCount)
                {
                    SendAllMailFunction(docent);
                }
                // 2. DB가 useCount보다 커서 메일을 모두 보내지 못한 경우
                else
                {
                    //a. 잔여 메일 보내기
                    if (sendMailCount >= useCount)
                        SendAllMailFunction(docent);
                    // b. 못 보낸 메일 수와, 시간 안내(test.Count가 0일수없음)
                    else
                    {
                        int differenceCount = sendMailCount - useCount;
                        DateTime listSendMail = DateTime.Parse(filterLogs[filterLogsCount - 1].TimeSentMail);
                        DateTime nextTimeSendMail = listSendMail.AddMinutes(30);
                        MessageBox.Show($"메일을 보낼 수 없습니다. {nextTimeSendMail} 이 후에 보내십시오.");
                        return false;
                    }
                }
            }
            return true;
        }

        private void SendAllMailFunction(DocentInformation docent)
        { 
            // Check Mail to
            //if (docent.Status.Color == System.Windows.Media.Colors.Red)
            //    return;
            
            if (docent.Type == "bookmark")
            {
                string htmlString = TSSManager.Instance.ParsingHTML(docent.RawDocentInformation);
                string image1 = TSSManager.Instance.ImagePath + @"\TechFair.png";
                string image2 = TSSManager.Instance.ImagePath + @"\WarningMessage.gif";
                
                var result = IMSmtp.SendSMTPBookmark(docent.MailTo, docent.RawDocentInformation.title, htmlString, image1, image2);
                if (result == "OK")
                {
                    this.Dispatcher.Invoke(() =>
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

                    this.Dispatcher.Invoke(() =>
                    {
                        docent.Status.Color = Colors.Red;
                    });

                    listDocent.Add(docent);
                    listError.Add(result);
                }
            }
            else
            {
                string htmlString = TSSManager.Instance.ParsingInquiryHTML(docent.InquiryInformation);
                string image1 = TSSManager.Instance.ImagePath + @"\TechFair.png";
                string image2 = TSSManager.Instance.ImagePath + @"\WarningMessage.gif";
                var result = IMSmtp.SendSMTPInquiry(docent.MailTo, docent.InquiryInformation.title, htmlString, image1, image2);
                if (result == "OK")
                {
                    this.Dispatcher.Invoke(() =>
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
                    this.Dispatcher.Invoke(() =>
                    {
                        docent.Status.Color = Colors.Red;
                    });
                    listDocent.Add(docent);
                    listError.Add(result);
                }
            }
        }



        #endregion


        private void SecondDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ChangeDate();
        }

        private void FirstDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ChangeDate();
        }
        private void ChangeDate()
        {
            string first = this.SearchPickDateView.FirstDate.Text;
            string second = this.SearchPickDateView.SecondDate.Text;

            if (first == string.Empty || second == string.Empty)
                return;

            string[] arrFirst = first.Split('-');
            string[] arrSecond = second.Split('-');

            DateTime dt_first = new DateTime(Convert.ToInt32(arrFirst[0]),
                                             Convert.ToInt32(arrFirst[1]),
                                             Convert.ToInt32(arrFirst[2]));

            DateTime dt_second = new DateTime(Convert.ToInt32(arrSecond[0]),
                                              Convert.ToInt32(arrSecond[1]),
                                              Convert.ToInt32(arrSecond[2]));

            // 값이 0보다 크면 dt_second 크고 0보다 작으면 dt_first 크다
            // 0이면 둘의 값이 동일 하다
            int result = DateTime.Compare(dt_second, dt_first);


            if (result > 0)
            {
                TSSManager.Instance.IsCheckPickDate = true;

                var docentInfors = TSSManager.Instance.GetAllInformation().DocentInformations.
                    FindAll(x => DateTime.Compare(DateTime.Parse(x.Date), dt_first) == 1 &&
                                 DateTime.Compare(DateTime.Parse(x.Date), dt_second) == -1);


                if (docentInfors.Count == 0)
                {
                    MainWindow main = (MainWindow)System.Windows.Application.Current.MainWindow;
                    main.main_grid.Children.Clear();
                    DocentInforListView diView = new DocentInforListView(0);
                    main.main_grid.Children.Add(diView);
                    return;
                }

                TSSManager.Instance.CheckSearchPage = 1;

                SetDocentInfor(docentInfors);
            }
            else if (result == 0)
            {
                MessageBox.Show("You have selected the same date. Please choose again.");

                this.SearchPickDateView.FirstDate.Text = "";
                this.SearchPickDateView.SecondDate.Text = "";
            }
            else
            {
                MessageBox.Show("Second dates cannot be larger than first dates. Please select again.");
                this.SearchPickDateView.SecondDate.Text = "";
            }
        }



        private void SearchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            //ComboBox 변경 될때 마다 Empty
            SearchTextBox.Text = "";
            _standardPage = 1;
            TSSManager.Instance.CheckSearchPage = 1;

            switch (SearchComboBox.SelectedItem)
            {
                case "Author":
                    SearchTextBox.Visibility = Visibility.Visible;
                    SearchPickDateView.Visibility = Visibility.Hidden;
                    TSSManager.Instance.IsCheckPickDate = false;
                    RestorationData();
                    break;

                case "Mail":
                    SearchTextBox.Visibility = Visibility.Visible;
                    SearchPickDateView.Visibility = Visibility.Hidden;
                    TSSManager.Instance.IsCheckPickDate = false;
                    RestorationData();
                    break;

                case "Date":
                    SearchTextBox.Visibility = Visibility.Hidden;
                    SearchPickDateView.Visibility = Visibility.Visible;
                    RestorationData();
                    break;
            }

        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DocentInforListView diView = (DocentInforListView)TSSManager.Instance.GetUserControl("Main_DocentInforListView");
            TextBox tb = sender as TextBox;


            SendAllButton.IsEnabled = false;
            // Send Button IsEnabled 체크
            //if (SearchTextBox.Text == string.Empty)
            //    this.SendAllButton.IsEnabled = true;

            if (tb.Text != string.Empty)
            {
                switch (SearchComboBox.SelectedItem)
                {
                    case "Author":
                        SearchAuthor(tb.Text);
                        break;

                    case "Mail":
                        SearchMail(tb.Text);
                        break;
                }
            }
            else
            {
                RestorationData();
                SendAllButton.IsEnabled = true;
            }


        }



        //Error ( 
        private async void SearchAuthor(string text)
        {
            await Task.Run(() =>
            {
                var docentInfors = TSSManager.Instance.GetAllInformation().DocentInformations.
                       FindAll(x => x.Author.Contains(text));
                
                this.Dispatcher.Invoke(() => 
                {
                    SetDocentUI(docentInfors);
                    if (docentInfors.Count != 0)
                        SetDocentInfor(docentInfors);
                });
            });
        }

        private async void SearchMail(string text)
        {
            await Task.Run(() =>
            {
                var docentInfors = TSSManager.Instance.GetAllInformation().DocentInformations.
                     FindAll(x => x.MailTo.Contains(text));

                this.Dispatcher.Invoke(() =>
                {
                    SetDocentUI(docentInfors);
                    if (docentInfors.Count != 0)
                        SetDocentInfor(docentInfors);
                });
            });
        }

        private void SetDocentUI(List<Model.DocentInformation> docentInfors)
        {
            if (docentInfors.Count == 0)
            {
                this.SendAllButton.IsEnabled = false;

                TSSManager.Instance.SearchDocentInfors = docentInfors;

                MainWindow main = (MainWindow)System.Windows.Application.Current.MainWindow;
                main.main_grid.Children.Clear();
                DocentInforListView diView = new DocentInforListView(0);
                main.main_grid.Children.Add(diView);
                return;
            }
            //else
            //    this.SendAllButton.IsEnabled = true;
            TSSManager.Instance.CheckSearchPage = 1;
        }

        private void SetDocentInfor(List<Model.DocentInformation> docentInfors)
        {
            // 5페이지 테스트
            //docentInfors.RemoveRange(98, 4);
            //docentInfors.RemoveRange(100, 2);
            //docentInfors.RemoveRange(18, 84);
            //docentInfors.RemoveRange(56, 46);

            TSSManager.Instance.SearchDocentInfors = docentInfors;
            int pageCount = docentInfors.Count / 20;
            int pageRemaining = docentInfors.Count % 20;
            if (pageRemaining != 0)
                pageCount += 1;


            // 버튼 활성화 비활성화


            List<PageNumberView> pageNumberViews = new List<PageNumberView>();
            for (int i = 0; i < pageCount; i++)
                pageNumberViews.Add(new PageNumberView(i + 1));


            /* Create PageNubmer */
            // 데이터 넣기
            TSSManager.Instance.SeachPageNumberViews = pageNumberViews;
            var cpv = (ChangePageView)TSSManager.Instance.GetUserControl("Main_ChangePageView");

            // Clear
            cpv.PageNumber.Children.Clear();

            // 5페이지 이상일 경우 5페이지만 생성
            if (pageCount > 5)
                pageCount = 5;

            // Add
            for (int i = 0; i < pageCount; i++)
                cpv.PageNumber.Children.Add(pageNumberViews[i]);

            // Select PageNumber
            foreach (var item in pageNumberViews)
                item.PNVBorder.Visibility = Visibility.Hidden;

            pageNumberViews[0].PNVBorder.Visibility = Visibility.Visible;


            /* DocentInforListView */
            MainWindow main = (MainWindow)System.Windows.Application.Current.MainWindow;
            main.main_grid.Children.Clear();
            DocentInforListView diView = new DocentInforListView(docentInfors);
            main.main_grid.Children.Add(diView);
        }
       
        // 1페이지 원상 복구
        private void RestorationData()
        {
            // 1페이지로 원상 복구
            MainWindow main = (MainWindow)System.Windows.Application.Current.MainWindow;
            main.main_grid.Children.Clear();

            int index = 1;
            DocentInforListView diView = new DocentInforListView(index);
            main.main_grid.Children.Add(diView);

            // GUI Selected
            for (int i = 0; i < TSSManager.Instance.PageNumberViews.Count; i++)
                TSSManager.Instance.PageNumberViews[i].PNVBorder.Visibility = Visibility.Hidden;
            TSSManager.Instance.PageNumberViews[index - 1].PNVBorder.Visibility = Visibility.Visible;


            // PageNumberView
            var cpv = (ChangePageView)TSSManager.Instance.GetUserControl("Main_ChangePageView");
            cpv.PageNumber.Children.Clear();

            //for (int i = 0; i < TSSManager.Instance.PageNumberViews.Count; i++)
            //    cpv.PageNumber.Children.Add(TSSManager.Instance.PageNumberViews[i]);
            TSSManager.Instance.MakePageNumberView();

            _standardPage = 1;
        }



        //-------------------------------------------------------- LoadButton
        private void LoadButton_Clicked(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            //ofd.InitialDirectory = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\JsonDatas";
            ofd.InitialDirectory = @"C:\imfine";
            ofd.Filter = "그림 파일 (*.json) | *.json; | 모든 파일 (*.*) | *.*";

            System.Windows.Forms.DialogResult dr = ofd.ShowDialog();

            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                string fileName = ofd.SafeFileName;
                string fileFullName = ofd.FileName;
                string filePath = fileFullName.Replace(fileName, "");

                //TSSManager.Instance.LoadJsonAESD(filePath); // 암호화 버전
                TSSManager.Instance.LoadJson(filePath);  // 

                this.SendAllButton.IsEnabled = true;
                // 스테이터스 표시
            }


            //System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            ////dialog.SelectedPath = @"C:\Users\basil\source\GIT\techfairsmtpsender\TechfairsSmtpSender\bin\Debug\net5.0-windows\JsonDatas";

            //dialog.SelectedPath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\JsonDatas";

            //dialog.ShowNewFolderButton = true;


            //if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    string select_path = dialog.SelectedPath;
            //    //Load
            //    TSSManager.Instance.LoadJson(select_path);
            //}
        }

        /// <summary>
        /// 1. Check Log DB(Today)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckLogDBButton_Clicked(object sender, RoutedEventArgs e)
        {
            // CheckLog 확인
            string path = TSSManager.Instance.imFineLogPath + @"\CheckLog.json";
            FileInfo fi = new FileInfo(path);
            if (fi.Exists == false)
            {
                MessageBox.Show("CheckLog.json 파일이 없습니다. 메일 전송 후 사용 하십시오.");
                return;
            }
                

            CheckLogWindowView clview = new CheckLogWindowView();
            clview.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
            clview.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            clview.Show();
        }
    }
}
