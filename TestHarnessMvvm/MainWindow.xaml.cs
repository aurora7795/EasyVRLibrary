using System.Windows;
using TestHarnessMvvm.ViewModel;

namespace TestHarnessMvvm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Closing += (s, e) => ViewModelLocator.Cleanup();
        }

        private void ResponseTb_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            //note: this is code behind which is generally frowned upon in MVVM but it is a View operation only - 
            // "Code behind on view is only bad when the operations needs dependencies past the View." 
            ResponseTb.ScrollToEnd();
        }
    }
}