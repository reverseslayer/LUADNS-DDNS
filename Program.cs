﻿using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LUADNS_DDNS {
    class Program {

        static string lastPublicIP = "";
        static async Task DDNSUpdateThread(string ZoneID, string UserName, string APIKEY, string currentIPRecord) {
            lastPublicIP = currentIPRecord;
            while (true) {
                string PublicIP = LUAddns.getPublicIP();
                Console.WriteLine("Checking if DNS is up to date");
                if (PublicIP != lastPublicIP) {
                    LUAddns.updateRecord(ZoneID, UserName, APIKEY, lastPublicIP, PublicIP);
                    lastPublicIP = PublicIP;
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
                Task x = DDNSUpdateThread(ZoneID, UserName, APIKEY, publicIP);
                Task.WaitAll(x);
            }

            if (CMD.ToLower() == "--help" || CMD.ToLower() == "/?") {

                Console.WriteLine(
@"
LUADNS - DDNS driver
Written by derek holloway

Help:
	luaddns Command ZoneID [/u UserName] [/k APIKey] [/i RecordID] [/a RecordIP]
	-- Note : Record ID updates after each succesful record update

Example:
	Get the records
		example: luaddns get 3 /u username /k 2523ab86fb8e8b8cb9

	Set the record
		example: luaddns set 3 /u username /k 2523ab86fb8e8b8cb9 /i 535333 /a 192.168.0.1
			- Sets the record 535333 to the ip 192.168.0.1
		example: luaddns set 3 /u username /k 2523ab86fb8e8b8cb9 /i 535333
			- Sets the record 535333 to the local computers public IP address
		
	Start DDNS updates
		example: luaddns ddns 3 /u username /k 2523ab86fb8e8b8cb9 /a 192.168.0.1
			- Starts the ddns server updating the record with the contents that match the IP specified
");

            }

        }
    }
}