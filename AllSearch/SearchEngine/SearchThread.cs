using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.System.Profile;

namespace AllSearch.SearchEngine
{
    class SearchThread
    {

        SearchCore core;
        string path, searchQuery;
        private Thread thread;

        public SearchThread(SearchCore searchCore, String path)
        {
            core = searchCore;
            searchQuery = core._configuration.searchQuery.ToLower();
            this.path = path;
            if (!MainWindow.isRunning)
            {
                return;
            }
            core._runningThreads.Add(this);
            thread = new Thread(runThread);
            thread.Start();
        }

        public void killThread()
        {
            if (thread != null && thread.IsAlive)
            {
                thread.Interrupt();
                thread.Join();
            }
        }

        public void runThread()
        {

            if (!Directory.Exists(path))
            {
                return;
            }

            List<string> searchData = new List<string>();
            try
            {
                if (core._configuration.inFiles)
                {
                    searchData.AddRange(Directory.GetFiles(path));
                }
                if (core._configuration.inFolders)
                {
                    searchData.AddRange(Directory.GetDirectories(path));
                }
                searchData.ForEach(path =>
                {
                    if (MainWindow.isRunning)
                    {
                        core.mainWindow.DispatcherQueue.TryEnqueue(() =>
                        {
                            core.textBlock.Text = path;
                        });
                    }
                    else
                    {
                        core.mainWindow.DispatcherQueue.TryEnqueue(() =>
                        {
                            core.textBlock.Text = "";
                        });
                    }
                    if (core._configuration.searchName)
                    {
                        if (File.Exists(path))
                        {
                            if (core._configuration.searchMode == SearchMode.REGEX)
                            {
                                if (System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(path), searchQuery))
                                {
                                    core._searchResults.Add(path);
                                }
                            }
                            else
                            if (Path.GetFileName(path).Contains(searchQuery) || Path.GetFileName(path).Equals(searchQuery))
                            {
                                core._searchResults.Add(path);
                            }
                        }
                        else if (Directory.Exists(path))
                        {

                            if (core._configuration.searchMode == SearchMode.REGEX)
                            {
                                if (System.Text.RegularExpressions.Regex.IsMatch(Path.GetDirectoryName(path), searchQuery))
                                {
                                    core._searchResults.Add(path);
                                }
                            }
                            else
                            if (Path.GetDirectoryName(path).Contains(searchQuery) || Path.GetDirectoryName(path).Equals(searchQuery))
                            {
                                core._searchResults.Add(path);
                            }
                        }


                    }
                    if (core._configuration.searchContent)
                    {
                        if (File.Exists(path))
                        {
                            byte[] buffer = new byte[20_000_000];
                            int bytesRead;
                            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                            {
                                if (!MainWindow.isRunning)
                                {
                                    return;
                                }
                                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    if (!MainWindow.isRunning)
                                    {
                                        break;
                                    }
                                    if (core._configuration.searchMode == SearchMode.REGEX)
                                    {
                                        if (System.Text.RegularExpressions.Regex.IsMatch(Encoding.UTF8.GetString(buffer, 0, bytesRead), searchQuery))
                                        {
                                            core._searchResults.Add(path);
                                            break;
                                        }
                                    }
                                    else
                                    if (Encoding.UTF8.GetString(buffer, 0, bytesRead).Contains(searchQuery) || Encoding.UTF8.GetString(buffer, 0, bytesRead).Equals(searchQuery))
                                    {
                                        core._searchResults.Add(path);
                                        break;
                                    }
                                }
                            }

                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            //check for argument out of range exception before removing
            try
            {
                core._runningThreads.Remove(this);
            }
            catch (Exception ex)
            {
                core._runningThreads.Clear();
            }

            searchData.Clear();
            //Free up memory
            searchData = null;

            if (core._searchResults.Count > 0)
            {
                core.mainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    core.resultButton.IsEnabled = true;
                });
            }



            core.mainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                core.resultButton.Content = "View Results: " + core._searchResults.Count;
            });
            try
            {
                Directory.GetDirectories(path).ToList().ForEach(folder =>
                {
                    while (core._runningThreads.Count > core._configuration.maxThreads && core._configuration.maxThreads != -1)
                    {
                        Thread.Sleep(1000);
                    }
                    if (!MainWindow.isRunning)
                    {
                        return;
                    }
                    Debug.WriteLine("Starting new thread " + core._runningThreads.Count);
                    new SearchThread(core, folder);
                });
                thread.Interrupt();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }





        }
    }
}
