using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
using TechfairsSmtpSender.ViewModel;

namespace TechfairsSmtpSender.View
{
    /// <summary>
    /// DocentInforListView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DocentInforListView : UserControl
    {

        private GridViewColumnHeader lastClickedGridViewColumnHeader = null;

        private ListSortDirection lastListSortDirection = ListSortDirection.Ascending;


        public DocentInforListView()
        {
            InitializeComponent();
            this.DataContext = new DocentInforListViewModel(1);

            ReadRawDataView rawDataView = (ReadRawDataView)TSSManager.Instance.GetUserControl("Main_ReadRawDataView");
            if (rawDataView != null)
                rawDataView.rawGrid.Children.Clear();
        }
        public DocentInforListView(int index)
        {
            InitializeComponent();
            this.DataContext = new DocentInforListViewModel(index);
            
            ReadRawDataView rawDataView = (ReadRawDataView)TSSManager.Instance.GetUserControl("Main_ReadRawDataView");
            if (rawDataView != null)
                rawDataView.rawGrid.Children.Clear();
        }

        public DocentInforListView(int index, int startCount, int endCount)
        {
            InitializeComponent();
            this.DataContext = new DocentInforListViewModel(index, startCount, endCount);
            
            ReadRawDataView rawDataView = (ReadRawDataView)TSSManager.Instance.GetUserControl("Main_ReadRawDataView");
            if (rawDataView != null)
                rawDataView.rawGrid.Children.Clear();
        }


        public DocentInforListView(List<DocentInformation> docents, int index = 0)
        {
            InitializeComponent();
            this.DataContext = new DocentInforListViewModel(docents, index);

            ReadRawDataView rawDataView = (ReadRawDataView)TSSManager.Instance.GetUserControl("Main_ReadRawDataView");
            if (rawDataView != null)
                rawDataView.rawGrid.Children.Clear();
        }





        private void sensor_listview_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }


        private void Sort(string header, ListSortDirection listSortDirection) 
        { 
            ICollectionView collectionView = CollectionViewSource.GetDefaultView(this.DocentListView.ItemsSource); 
            collectionView.SortDescriptions.Clear(); SortDescription sortDescription = new SortDescription(header, listSortDirection); 
            collectionView.SortDescriptions.Add(sortDescription); 
            collectionView.Refresh(); 
        }


        private void gridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader clickedGridViewColumnHeader = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection listSortDirection;

            if (clickedGridViewColumnHeader != null)
            {
                if (clickedGridViewColumnHeader.Role != GridViewColumnHeaderRole.Padding)
                { 
                    // 마지막으로 클릭한 그리브 뷰 컬럼 헤더가 아닌 경우
                    if(clickedGridViewColumnHeader != this.lastClickedGridViewColumnHeader) 
                    {
                        listSortDirection = ListSortDirection.Ascending; 
                    } 
                    else // 마지막으로 클릭한 그리드 뷰 컬럼 헤더인 경우
                    { 
                        if(this.lastListSortDirection == ListSortDirection.Ascending)
                        { 
                            listSortDirection = ListSortDirection.Descending; 
                        } 
                        else 
                        { 
                            listSortDirection = ListSortDirection.Ascending; 
                        } 
                    } 
                    string header = clickedGridViewColumnHeader.Column.Header as string; 
                    Sort(header, listSortDirection); 
                    this.lastClickedGridViewColumnHeader = clickedGridViewColumnHeader; 
                    this.lastListSortDirection = listSortDirection; 
                }
            }
        }
    }
}
