using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using Newtonsoft.Json;

namespace NonEmergencyBot.LUIS
{
    public class LuisContext
    {
        public const string QueryUri = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/" +
            "c00dcea1-260f-4fe9-b3aa-13abff2f2d22?subscription-key=a78c19171ea841e6a80961c8c6ce6e6f" +
            "&verbose=true&timezoneOffset=0&q=";

        public string CurrentQuery { get; set; }

        public LUISMap CurrentResponse { get; set; }
        
        public LuisContext(string query)
        {
            CurrentQuery = QueryUri + Uri.EscapeUriString(query);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(CurrentQuery);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        CurrentResponse = JsonConvert.DeserializeObject<LUISMap>(reader.ReadToEnd());
                    }
                }
            }
        }
    }
}