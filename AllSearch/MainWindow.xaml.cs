using AllSearch.SearchEngine;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using Windows.Graphics;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using WinRT;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AllSearch
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        private WindowsSystemDispatcherQueueHelper m_wsdqHelper;
        private MicaController m_backdropController;
        private SystemBackdropConfiguration m_configurationSource;
        private AppWindow _apw;
        private OverlappedPresenter presenter;

        public void GetAppWindowAndPresenter()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            _apw = AppWindow.GetFromWindowId(myWndId);
            presenter = _apw.Presenter as OverlappedPresenter;
        }

        public MainWindow()
        {
            GetAppWindowAndPresenter();
            this.InitializeComponent();
            this.Title = "AllSearch";
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo d in allDrives)
            {
                if (d.IsReady == true)
                {
                    driveSelectionBox.Items.Add(d.Name);
                }
            }
            driveSelectionBox.Items.Add("Choose Custom");
            SizeInt32 sizeInt32 = new Windows.Graphics.SizeInt32();
            sizeInt32.Width = 800;
            sizeInt32.Height = 500;
            _apw.Resize(sizeInt32);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            TrySetSystemBackdrop();
        }

        bool TrySetSystemBackdrop()
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                // Create the policy object.
                m_configurationSource = new SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_backdropController = new Microsoft.UI.Composition.SystemBackdrops.MicaController();

                // Enable the system backdrop.
                // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                m_backdropController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_backdropController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // succeeded
            }

            return false; // Mica is not supported on this system
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed
            // so it doesn't try to use this closed window.
            if (m_backdropController != null)
            {
                m_backdropController.Dispose();
                m_backdropController = null;
            }
            this.Activated -= Window_Activated;
            m_configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default; break;
            }
        }

        public static bool isRunning = false;
        private SearchCore searchCore;

        private async void searchButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isRunning)
            {
                SearchMode searchMode = SearchMode.STRING;
                if (((ComboBox)searchModeCombo).SelectedItem.Equals("Regex"))
                { searchMode = SearchMode.REGEX; }

                string value;
                searchValue.Document.GetText(Microsoft.UI.Text.TextGetOptions.AdjustCrlf, out value);
                //Start searchEngine in new Thread

                int threads = multithreadingBox.IsChecked.Value ? (boostBox.IsChecked.Value && boostBox.IsEnabled) ? 300 : 50 : 1;
                if (value.Length == 0)
                {
                    ContentDialog dialog = new ContentDialog()
                    {
                        Title = "Error",
                        Content = "SearchFor is empty!",
                        CloseButtonText = "Ok"
                    };
                    dialog.XamlRoot = this.Content.XamlRoot;
                    await dialog.ShowAsync();
                    return;
                }
                if (!searchLocationFiles.IsChecked.Value && !searchLocationFolders.IsChecked.Value)
                {
                    ContentDialog dialog = new ContentDialog()
                    {
                        Title = "Error",
                        Content = "No searchLocation selected!",
                        CloseButtonText = "Ok"
                    };
                    dialog.XamlRoot = this.Content.XamlRoot;
                    await dialog.ShowAsync();
                    return;
                }
                if (!searchInConent.IsChecked.Value && !searchInName.IsChecked.Value)
                {
                    ContentDialog dialog = new ContentDialog()
                    {
                        Title = "Error",
                        Content = "No searchIn selected!",
                        CloseButtonText = "Ok"
                    };
                    dialog.XamlRoot = this.Content.XamlRoot;
                    await dialog.ShowAsync();
                    return;
                }
                searchCore = new SearchCore(new SearchConfiguration(value, searchLocationFolders.IsChecked.Value, searchLocationFiles.IsChecked.Value, searchInConent.IsChecked.Value, searchInName.IsChecked.Value, searchMode, (string)((ComboBox)driveSelectionBox).SelectedItem, threads));
                searchCore.startSearch(this);
                searchButton.Content = "Stop Search";
            }
            else
            {
                searchCore.stopSearch();
                searchButton.Content = "Start Search";
                statusLabel.Text = "";
            }

        }



        private void searchLocationFiles_Checked(object sender, RoutedEventArgs e)
        {
            if (searchLocationFolders.IsChecked.Value)
            {
                searchInConent.IsEnabled = true;
            }
        }

        private void searchLocationFolders_Checked(object sender, RoutedEventArgs e)
        {
            if (!searchLocationFiles.IsChecked.Value)
            {
                searchInConent.IsEnabled = false;
            }
        }

        private void searchLocationFolders_Unchecked(object sender, RoutedEventArgs e)
        {
            searchInConent.IsEnabled = true;
        }

        private void searchLocationFiles_Unchecked(object sender, RoutedEventArgs e)
        {
            if (searchLocationFolders.IsChecked.Value)
            {
                searchInConent.IsEnabled = false;
            }
        }

        private async void multithreadingBox_Checked(object sender, RoutedEventArgs e)
        {

            //Are you sure?

            ContentDialog dialog = new ContentDialog()
            {
                Title = "Warning",
                Content = "Multithreading may cause a higher CPU load but can speed up the process.\n\nDo you want to continue?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No"
            };
            dialog.XamlRoot = this.Content.XamlRoot;
            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
            {
                multithreadingBox.IsChecked = false;
            }
            else
            {
                boostBox.IsEnabled = true;
            }

        }

        private void resultsButton_Click(object sender, RoutedEventArgs e)
        {
            ResultsWindow resultsWindow = new ResultsWindow(searchCore._searchResults);
            resultsWindow.Show();
        }

        private async void boostBox_Checked(object sender, RoutedEventArgs e)
        {
            //Are you sure you want to enable boost?
            ContentDialog dialog = new ContentDialog()
            {
                Title = "Warning",
                Content = "Thread Boosting may cause a higher Memory load but can speed up the process.\n\nDo you want to continue?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No"
            };
            dialog.XamlRoot = this.Content.XamlRoot;
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                boostBox.IsChecked = false;
            }

        }

        private void multithreadingBox_Unchecked(object sender, RoutedEventArgs e)
        {
            boostBox.IsEnabled = false;
        }

        private async void driveSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (driveSelectionBox.SelectedValue.Equals("Choose Custom"))
            {
                var folderPicker = new FolderPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
                folderPicker.FileTypeFilter.Add("*");
                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder == null)
                {
                    driveSelectionBox.SelectedIndex = 0;
                    return;
                }
                driveSelectionBox.Items.Add(folder.Path);
                driveSelectionBox.SelectedItem = folder.Path;
                driveSelectionBox.Items.Remove("Choose Custom");
                driveSelectionBox.Items.Add("Choose Custom");
            }

        }
    }
}
class WindowsSystemDispatcherQueueHelper
{
    [StructLayout(LayoutKind.Sequential)]
    struct DispatcherQueueOptions
    {
        internal int dwSize;
        internal int threadType;
        internal int apartmentType;
    }

    [DllImport("CoreMessaging.dll")]
    private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

    object m_dispatcherQueueController = null;
    public void EnsureWindowsSystemDispatcherQueueController()
    {
        if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
        {
            // one already exists, so we'll just use it.
            return;
        }

        if (m_dispatcherQueueController == null)
        {
            DispatcherQueueOptions options;
            options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
            options.threadType = 2;    // DQTYPE_THREAD_CURRENT
            options.apartmentType = 2; // DQTAT_COM_STA

            CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
        }
    }
}