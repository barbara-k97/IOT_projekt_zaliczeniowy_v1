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

          Console.WriteLine("Aeeee");
          Console.WriteLine("Aeeee");
          Console.WriteLine("Aeeee");
          Console.WriteLine("Aeeee");
          Console.WriteLine("Aeeee");
          Console.WriteLine("Aeeee");
          Console.WriteLine("Aeeee");


          Console.WriteLine("AgentForFabric zatrzymana   ");
          Console.ReadLine();

     }
}
