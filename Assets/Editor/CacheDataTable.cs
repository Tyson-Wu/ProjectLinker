using System;
using System.Collections.Generic;
namespace ProjectLinker
{
    [Serializable]
    class CacheDataTable
    {
        public List<CacheDataElem> elems = new List<CacheDataElem>();
        public CacheDataTable()
        {
            List<CacheDataElem> elems = new List<CacheDataElem>();
        }
    }
    [Serializable]
    class CacheDataElem
    {
        public string name;
        public string buildTarget;
        public string projectPath;
    }
}