using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NonEmergencyBot
{
    public enum BotState
    {
        None,
        Name,
        DateOfBirth,
        AskIssue,
        Location,
        Issue_Theft,
        Issue_Assault,
        Issue_Harassment,
        Issue_CarCrash,
        Issue_CriminalDamage,
        Issue_Information,
        Issue_None,
        ContactDetails,
    }
}