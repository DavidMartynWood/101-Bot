using System;
using System.IO;
using System.Net;

using Newtonsoft.Json;

namespace NonEmergencyBot.LUIS
{
    [Serializable]
    public class LuisContext
    {
        public readonly string QueryUri = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/" +
            PrivateKeys.LuisAppId +
            "?subscription-key=" +
            PrivateKeys.LuisApiKey +
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
                        string rawJson = reader.ReadToEnd();
                        CurrentResponse = JsonConvert.DeserializeObject<LUISMap>(rawJson);
                    }
                }
            }
        }
    }
}