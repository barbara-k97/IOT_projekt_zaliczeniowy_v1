﻿using System.Net.Sockets;
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


          // prośba o podanie ścieżki URL do serwera OPC UA 
          // opc.tcp://localhost:4840/

          Console.WriteLine("Podaj sciezke URL do serwera OPC UA  : ");
          string adresServerOPC = Console.ReadLine() ?? string.Empty;


          using (var client = new OpcClient(adresServerOPC))
          {
               client.Connect();
               Console.WriteLine("OPC UA Łączenie zakończone sukcesem !");

               var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder);
               List<String> devicesList = ReadDeviceFromSimulator(node);

               var device = new Class1(deviceClient, client);
               await device.InitializeHandlers();

               while (devicesList.Count> 0 )
               {

                    //lista commands
                    List<OpcReadNode> commands = new List<OpcReadNode>();
                    Console.WriteLine("---------");

                    // Tworzenie listy węzłów OPC na podstawie wczytanych nazw urządzeń
                    foreach (string deviceName in devicesList)
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
                              nameDev = name.Value,
                              ProductionStatus = ProductionS.Value,
                              WorkorderId = WorkorderId.Value,
                              GoodCount = GoodCount.Value,
                              BadCount = BadCount.Value,
                              Temperature = Temperature.Value,
                              ProductionRate = ProductionRate.Value,
                              DeviceErrors = DeviceErrors.Value
                         };


                         
                        // Console.WriteLine(data);
                         Console.WriteLine("___________________");
                         await device.SendTelemetry(deviceName, WorkorderId.Value, ProductionS.Value,  Temperature.Value,  ProductionRate.Value,  GoodCount.Value,  BadCount.Value,  DeviceErrors.Value);
                  
                         Console.WriteLine("___________________");
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
                              Console.WriteLine($" DEVICE {devicesList[numerUrzadzenia]}  ");
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

                    // czekanie az wszystkie device zostaną sprawdzone, dane zebrane i wtedy dopiero
                    await Task.Delay(10000); //10000milisekund = 10sekund 
               }
               client.Disconnect();
          }

          Console.WriteLine("AgentForFabric zatrzymana   ");
          Console.ReadLine();

     }
  

     // LISTA DO POBRANIA NAZW DEVICE  I ILE ICH JEST  W SYMULATORXE 

     //czytanie nazw Device z symulatora
     static List<String> ReadDeviceFromSimulator(OpcNodeInfo node, int numberDevice = 0)
     {
          List<String> deviceNames = new List<String>();
          numberDevice++;
          //Console.WriteLine(" !    Lista urzadzen:" );
          foreach (var childNode in node.Children())
          {
               if (childNode.DisplayName.Value.Contains("Device "))
               {
                    Console.WriteLine("  Device:" + childNode.DisplayName.Value);
                    deviceNames.Add(childNode.DisplayName.Value);
               }
               ReadDeviceFromSimulator(childNode, numberDevice);
          }
          return deviceNames;
     }

}