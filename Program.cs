﻿using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LUADNS_DDNS {
    class Program {

        static void WriteToFile(string FilePath, string input) {
            using (FileStream file = File.OpenWrite(FilePath + "/lastip")) {
                byte[] data = Encoding.UTF8.GetBytes(input);
                file.Write(data, 0, data.Length);
            }
        }

        static string ReadFromFile(string FilePath) {
            using (FileStream file = File.OpenRead(FilePath + "/lastip")) {
                byte[] data = new byte[10000];
                int bytesRead = file.Read(data);
                return Encoding.UTF8.GetString(data.Sub(0, bytesRead));

            }
        }

        static string lastPublicIP = "";
        static async Task DDNSUpdateThread(string ZoneID, string UserName, string APIKEY, string currentIPRecord, string FilePath) {
            if (string.IsNullOrEmpty(currentIPRecord)) {
                lastPublicIP = ReadFromFile(FilePath);
            } else {
                lastPublicIP = currentIPRecord;
            }
            while (true) {
                try {
                    string PublicIP = await LUAddns.getPublicIPAsync();
                    Console.WriteLine("Checking if DNS is up to date");
                    WriteToFile(FilePath, PublicIP);
                    if (PublicIP != lastPublicIP) {
                        LUAddns.updateRecord(ZoneID, UserName, APIKEY, lastPublicIP, PublicIP);
                        lastPublicIP = PublicIP;
                        Console.WriteLine("The public IP has changed to : " + PublicIP);
                    } else {
                        Console.WriteLine("The public IP is up to date");
                    }
                }catch(Exception e) {
                    Console.WriteLine("An error has occured : " + e.ToString());
                    Console.WriteLine("Waiting till next update cycle to try again");
                }
                await Task.Delay(1000 * 60 * 60); // Sleep the thread for an hour
            }
        }

        static void Main(string[] args) {
            string CMD = args.Length >= 1 ? args[0].ToLower() : "";
            string ZoneID = args.Length >= 2 ? args[1].ToLower() : "";
            string UserName = "";
            string APIKEY = "";
            string publicIP = "";
            string FilePath = "";
            int RecordID = 0;

            if (args.Length >= 3) {
                for (int i = 0; i < args.Length; i++) {
                    string arg = args[i];
                    if (arg == "/u" || arg == "-u") {
                        UserName = args[i + 1];
                    } else if (arg == "/k" || arg == "-k") {
                        APIKEY = args[i + 1];
                    } else if (arg == "/i" || arg == "-i") {
                        RecordID = Convert.ToInt32(args[i + 1]);
                    } else if (arg == "/a" || arg == "-a") {
                        publicIP = args[i + 1];
                    } else if (arg == "/p" || arg == "-p") {
                        FilePath = args[i + 1];
                    }
                }
            }

            if (CMD.ToLower() == "get") {
                LUAddns.printRecords(ZoneID, UserName, APIKEY);
            }

            if (CMD.ToLower() == "set") {
                ResponsePacket data = LUAddns.getRecords(ZoneID, UserName, APIKEY);
                string oldip = "";
                foreach (RecordPacket item in data.records) {
                    if (item.id == RecordID) {
                        oldip = item.content;
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(oldip)) {
                    LUAddns.updateRecord(ZoneID, UserName, APIKEY, oldip, publicIP);
                } else {
                    Console.WriteLine("The Record-ID " + RecordID + " was not found");
                }
            }

            if (CMD.ToLower() == "ddns") {
                Task x = DDNSUpdateThread(ZoneID, UserName, APIKEY, publicIP, FilePath);
                Task.WaitAll(x);
            }

            if (CMD.ToLower() == "--help" || CMD.ToLower() == "/?") {

                Console.WriteLine(
@"
LUADNS - DDNS driver
Written by derek holloway

Help:
	luaddns Command ZoneID [/u UserName] [/k APIKey] [/i RecordID] [/a RecordIP] [/p FolderPath]
	    Command     - get, set, or ddns
        ZoneID      - get it from the url https://api.luadns.com/zones/53755 where 53755 is the zone
        APIKey      - get it from https://api.luadns.com/settings, scroll down to API Token
        RecordID    - get it from running the command get first - NOTE: the record id changes every update
        RecordIP    - This is the value of the A record
        FolderPath  - This is the path of the folder you want to use to store logs and data

Example:
	Get the records
		luaddns get 3 /u username /k 2523ab86fb8e8b8cb9

	Set the record
		luaddns set 3 /u username /k 2523ab86fb8e8b8cb9 /i 535333 /a 192.168.0.1
			- Sets the record 535333 to the ip 192.168.0.1
		luaddns set 3 /u username /k 2523ab86fb8e8b8cb9 /i 535333
			- Sets the record 535333 to the local computers public IP address
		
	Start DDNS updates
		luaddns ddns 3 /u username /k 2523ab86fb8e8b8cb9 /p C:\Users\public\LUADDNS /a 192.168.0.1
			- Starts the ddns server updating the record with the contents that match the IP specified
        luaddns ddns 3 /u username /k 2523ab86fb8e8b8cb9 /p C:\Users\public\LUADDNS
            - Starts the ddns server updating the record with the contents that match the IP in the folder specified
");

            }

        }
    }

    static class Extensions {
        public static T[] Sub<T>(this T[] data, int index, int length) {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }
}
