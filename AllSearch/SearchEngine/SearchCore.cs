using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AllSearch.SearchEngine
{
    class SearchCore
    {

        public SearchConfiguration _configuration;
        public List<string> _searchResults = new List<string>();
        public List<SearchThread> _runningThreads = new List<SearchThread>();
        public MainWindow mainWindow;
        private ProgressRing progressRing;
        public TextBlock textBlock;
        public Button resultButton;
        public SearchCore(SearchConfiguration searchConfiguration)
        {
            _configuration = searchConfiguration;
        }

        public void startSearch(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            progressRing = (ProgressRing)((FrameworkElement)mainWindow.Content).FindName("processRing");
            this.textBlock = (TextBlock)((FrameworkElement)mainWindow.Content).FindName("statusLabel");
            progressRing.IsActive = true;
            resultButton = (Button)((FrameworkElement)mainWindow.Content).FindName("resultsButton");
            resultButton.IsEnabled = false;
            resultButton.Content = "View Results: 0";
            MainWindow.isRunning = true;
            new SearchThread(this, _configuration.startPath);


        }

        public void stopSearch()
        {
            MainWindow.isRunning = false;
            List<SearchThread> searchThreadsTemp = new List<SearchThread>(_runningThreads);
            foreach (SearchThread thread in searchThreadsTemp)
            {
                if (thread != null)
                    thread.killThread();
            }
            _runningThreads.Clear();
            progressRing.IsActive = false;
            textBlock.Text = "";
        }

    }

}
