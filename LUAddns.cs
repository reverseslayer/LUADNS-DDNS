using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LUADNS_DDNS {

    class LUAddns {
        public static void updateRecord(string ZoneID, string UserName, string APIKEY, string OldIP, string? NewIP) {
            ResponsePacket data = getRecords(ZoneID, UserName, APIKEY);
            foreach (RecordPacket cur in data.records) {
                if (cur.content == OldIP) {
                    cur.content = string.IsNullOrEmpty(NewIP) ? getPublicIP() : NewIP;
                    break;
                }
            }
            setRecords(ZoneID, UserName, APIKEY, data);
        }
        public static void printRecords(string ZoneID, string UserName, string APIKEY) {
            try {
                ResponsePacket data = getRecords(ZoneID, UserName, APIKEY);
                foreach (RecordPacket cur in data.records) {
                    Console.WriteLine(cur.id + " =\t[ Name : " + cur.name + " ]\t[ Record-Type : " + cur.type + " ]\t[ Record-Data : " + cur.content + " ]");
                }
                Console.WriteLine("");
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }
        public static ResponsePacket getRecords(string ZoneID, string UserName, string APIKEY) {
            try {
                return JsonConvert.DeserializeObject<ResponsePacket>(HTTPGet(ZoneID, UserName, APIKEY));
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return null;
            }
        }
        public static bool setRecords(string ZoneID, string UserName, string APIKEY, ResponsePacket Records) {
            try {
                string result = HTTPPut(ZoneID, JsonConvert.SerializeObject(Records), UserName, APIKEY);
                return true;
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return false;
            }
            
        }
        static string HTTPGet(string ZoneID, string Uname, string ApiKey) {
            using (WebClient client = new WebClient()) {
                client.Headers.Add("Accept", "application/json");
                client.Credentials = new NetworkCredential(Uname, ApiKey);
                using (StreamReader stream = new StreamReader(client.OpenRead("https://api.luadns.com/v1/zones/" + ZoneID))) {
                    return stream.ReadToEnd();
                }
            }
        }
        static string HTTPPut(string ZoneID, string JSONData, string Uname, string ApiKey) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.luadns.com/v1/zones/" + ZoneID);
            request.Method = "PUT";
            request.Credentials = new NetworkCredential(Uname, ApiKey);
            request.Headers.Add("Accept", "application/json");
            using (Stream stream = request.GetRequestStream()) {
                byte[] data = Encoding.UTF8.GetBytes(JSONData);
                stream.Write(data, 0, data.Length);
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (StreamReader stream = new StreamReader(response.GetResponseStream())) {
                return stream.ReadToEnd();
            }
        }
        public static string getPublicIP() {
            return new WebClient().DownloadString("https://ipv4.icanhazip.com/").TrimEnd();
        }

        public static async Task<string> getPublicIPAsync() {
            string x = await new WebClient().DownloadStringTaskAsync("https://ipv4.icanhazip.com/");
            return x.TrimEnd();
        }
    }
    
    public class ResponsePacket {
        public int id;
        public string name;
        public int template_id;
        public bool synced;
        public int queries_count;
        public int records_count;
        public int aliases_count;
        public int redirects_count;
        public int forwards_count;
        public RecordPacket[] records;
    }

    public class RecordPacket {
        public int id;
        public string name;
        public string type;
        public string content;
        public int ttl;
        public int zone_id;
        public bool generated;
        public DateTime created_at;
        public DateTime updated_at;
    }

}
