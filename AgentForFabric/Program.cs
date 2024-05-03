using System.Net.Sockets;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Client;
using Opc.UaFx;
using Opc.UaFx.Client;
using Library;

class Program
{
     static async Task Main(string[] args)
     {

          Console.WriteLine("Witaj w AgentForFabric ! ");
          Console.WriteLine("-------------------------------------------------");


      
          // AZURE podpięcie się do urządzenia w hubZajecia o nazwie test_device
          Console.WriteLine(" !!!            ---  Łączenie z  Azure !");
          // String do połączenia z Azure wpisane 
          Console.WriteLine("Wpisz string do połączenia z Azure  ( Device Numbers ) : ");
          string deviceConnectionString = Console.ReadLine() ?? string.Empty;
          using var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
          await deviceClient.OpenAsync();
          Console.WriteLine(" !!!          Łączenie z Azure zakończone sukcesem !");

        
          
          
          // ŁĄCZENIE I POBIERANIE DANYCH Z OPC UA 


          //  nazwa pliku json z device 
           string jsonFilePath = Path.Combine("device_names.json");  //ścieżka do pliku w bin->net6.0


          // Wczytanie nazw urządzeń z pliku JSON do listy 
          List<string> deviceNames = ReadDeviceNamesFromJson(jsonFilePath);

          // prośba o podanie ścieżki URL do serwera OPC UA 
          // opc.tcp://localhost:4840/

          Console.WriteLine("Podaj sciezke URL do serwera OPC UA  : ");
          string adresServerOPC = Console.ReadLine() ?? string.Empty;

      

          using (var client = new OpcClient(adresServerOPC))
          {
               client.Connect();
               Console.WriteLine("OPC UA Łączenie zakończone sukcesem !");
               OpcValue PS1 = client.ReadNode($"ns=2;s=Device 1/ProductionStatus");
               OpcValue PS2 = client.ReadNode($"ns=2;s=Device 2/ProductionStatus");
               OpcValue PS3 = client.ReadNode($"ns=2;s=Device 3/ProductionStatus");

               //lista commands
               List<OpcReadNode> commands = new List<OpcReadNode>();


                    var device = new Class1(deviceClient, client);
                    Console.WriteLine("---------");

               while (deviceNames.Count  >0 )
               {


                    // Tworzenie listy węzłów OPC na podstawie wczytanych nazw urządzeń
                    foreach (string deviceName in deviceNames)
                    {


                         OpcValue name = deviceName;
                         commands.Add(new OpcReadNode("ns=2;s=" + deviceName + "/ProductionStatus", OpcAttribute.DisplayName));
                         commands.Add(new OpcReadNode("ns=2;s=" + deviceName + "/ProductionStatus"));
                         OpcValue ProductionS = client.ReadNode("ns=2;s=" + deviceName + "/ProductionStatus");

                         commands.Add(new OpcReadNode("ns=2;s=" + deviceName + "/ProductionRate", OpcAttribute.DisplayName));
                         commands.Add(new OpcReadNode("ns=2;s=" + deviceName + "/ProductionRate"));
                         OpcValue ProductionRate = client.ReadNode("ns=2;s=" + deviceName + "/ProductionRate");

                         commands.Add(new OpcReadNode("ns=2;s=" + deviceName + "/WorkorderId", OpcAttribute.DisplayName));
                         commands.Add(new OpcReadNode("ns=2;s=" + deviceName + "/WorkorderId"));
                         OpcValue WorkorderId = client.ReadNode("ns=2;s=" + deviceName + "/WorkorderId");

                         commands.Add(new OpcReadNode("ns=2;s=" + deviceName + "/Temperature", OpcAttribute.DisplayName));
                         commands.Add(new OpcReadNode("ns=2;s=" + deviceName + "/Temperature"));
                         OpcValue Temperature = client.ReadNode("ns=2;s=" + deviceName + "/Temperature");

                         commands.Add(new OpcReadNode("ns=2;s=" + deviceName + "/GoodCount", OpcAttribute.DisplayName));
                         commands.Add(new OpcReadNode("ns=2;s=" + deviceName + "/GoodCount"));
                         OpcValue GoodCount = client.ReadNode("ns=2;s=" + deviceName + "/GoodCount");

                         commands.Add(new OpcReadNode("ns=2;s=" + deviceName + "/BadCount", OpcAttribute.DisplayName));
                         OpcValue BadCount = client.ReadNode("ns=2;s=" + deviceName + "/BadCount");
                         commands.Add(new OpcReadNode("ns=2;s=" + deviceName + "/BadCount"));
                         commands.Add(new OpcReadNode("ns=2;s=" + deviceName + "/DeviceError", OpcAttribute.DisplayName));
                         commands.Add(new OpcReadNode("ns=2;s=" + deviceName + "/DeviceError"));
                         OpcValue DeviceErrors = client.ReadNode("ns=2;s=" + deviceName + "/DeviceError");



                         var data = new
                         {
                              name = name.Value,
                              ProductionStatus = ProductionS.Value,
                              WorkorderId = WorkorderId.Value,
                              GoodCount = GoodCount.Value,
                              BadCount = BadCount.Value,
                              Temperature = Temperature.Value,
                              ProductionRate = ProductionRate.Value,
                         };
                         Console.WriteLine(data);



                         Console.WriteLine("___________________");


                         await device.SendTelemetry(data);

                    }
               }

                    IEnumerable<OpcValue> job = client.ReadNodes(commands.ToArray());

                    // Wypisywanie 
                    // 7 wartosci kazdy zajmuje co 2 linijki, 
                    // co 15 linijke bedzie nowe urzadzenie 
                    int numer = 0;
                    int numerUrzadzenia = 0;
                    foreach (var item in job)
                    {
                         if (numer % 14 == 0)
                         {

                              Console.WriteLine($" DEVICE {deviceNames[numerUrzadzenia]}  ");
                              numerUrzadzenia++;
                         }


                         // wyswietla wartosc dla urzadzenia
                         Console.WriteLine(item.Value);

                         if ((numer + 1) % 14 == 0)
                         {

                              Console.WriteLine("___________________");

                         }

                         numer++;

                    }


                Console.ReadKey();

               //client.Disconnect();
          }




          static List<string> ReadDeviceNamesFromJson(string filePath)
          {
               List<string> deviceNames = new List<string>();

               try
               {
                    // Wczytanie zawartości pliku JSON
                    string jsonContent = File.ReadAllText(filePath);
                    // Deserializacja zawartości JSON do obiektu
                    var jsonObject = JsonConvert.DeserializeObject<dynamic>(jsonContent);
                    // Pobranie listy nazw urządzeń z obiektu JSON
                    foreach (var deviceName in jsonObject.deviceNames)
                    {
                         deviceNames.Add(deviceName.ToString());
                    }
                    Console.WriteLine("####");
               }
               catch (Exception ex)
               {
                    Console.WriteLine($"Nie udało sie odczytać zawartości w pliku JSON: {ex.Message}");
               }
               return deviceNames;
          }





          Console.WriteLine("AgentForFabric zatrzymana   ");
          Console.ReadLine();

     }
}
