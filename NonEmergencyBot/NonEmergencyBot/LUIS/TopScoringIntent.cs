using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NonEmergencyBot.LUIS
{
    public class TopScoringIntent
    {
        public string Intent { get; set; }
        public float Score { get; set; }
    }
}