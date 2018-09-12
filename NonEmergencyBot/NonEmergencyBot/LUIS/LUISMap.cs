using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NonEmergencyBot.LUIS
{
    public class LUISMap
    {
        public string Query { get; set; }
        public TopScoringIntent TopScoringIntent { get; set; }
    }
}