using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllSearch.SearchEngine
{
    class SearchConfiguration
    {

        public bool inFolders, inFiles, searchContent, searchName;
        public string searchQuery,startPath;
        public SearchMode searchMode;
        public int maxThreads;
        
        public SearchConfiguration(string query, bool folders, bool files, bool content, bool name, SearchMode searchMode,string startPath,int maxThreads)
        {
            searchQuery = query;
            this.startPath = startPath;
            inFolders = folders;
            inFiles = files;
            searchContent = content;
            searchName = name;
            this.searchMode = searchMode;
            this.maxThreads = maxThreads;
        }   
    }
    enum SearchMode
    {
        REGEX,
        STRING
    }
}
