using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DXHealthBot
{
    public class GraphAPI
    {
    }


    public class Rootobject
    {
        public string odatacontext { get; set; }
        public string odatanextLink { get; set; }
        public Value[] value { get; set; }
    }

    public class Value
    {
        public string odataetag { get; set; }
        public string id { get; set; }
        public string subject { get; set; }
    }

}