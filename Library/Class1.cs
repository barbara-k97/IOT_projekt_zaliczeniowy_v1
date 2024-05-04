using Newtonsoft.Json;
using Microsoft.Azure.Devices.Client;
using Opc.UaFx;
using Opc.UaFx.Client;
using System.Net.Mime;
using System.Text;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client.Exceptions;


namespace Library
{
     public class Class1
     {
          private DeviceClient client;
          private OpcClient OPC;


          public Class1(DeviceClient deviceClient, OpcClient OPC)
          {
               this.client = deviceClient;
               this.OPC = OPC;
          }

          /*
                    #region D2C - Sending telemetry
                    public async Task SendTelemetry(dynamic data)
                    {
                         Console.WriteLine(data);

                         var selectedData = new
                         {
                              //nazwa = data.name,
                              //ProductionStatus = data.ProductionStatus,
                              WorkorderId = data.WorkorderId,
                              Temperature = data.Temperature,
                              ProductionRate = data.ProductionRate,
                              GoodCount = data.GoodCount,
                              BadCount = data.BadCount,
                         };

                         var dataString = JsonConvert.SerializeObject(selectedData);
                         Console.WriteLine(dataString);
                         Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
                         eventMessage.ContentType = MediaTypeNames.Application.Json;
                         eventMessage.ContentEncoding = "utf-8";
                         await client.SendEventAsync(eventMessage);
                         if (true)
                              await Task.Delay(2000); //20sekund

                         Console.WriteLine(" Koniec wywołania metody SendTelemetry z Library ");

                    }
                    #endregion

          */


          #region D2C - Sending telemetry
          public async Task SendTelemetry(dynamic data)
          {
               //Console.WriteLine(data);
               /*    / var selectedData = new
                    {
                         DeviceName = data.name,
                         WorkorderId = data.WorkorderId,
                         ProductionStatus = data.ProductionStatus,
                         Temperature = data.Temperature,
                         ProductionRate = data.ProductionRate,
                         GoodCount = data.GoodCount,
                         BadCount = data.BadCount,
                         DeviceErrors = data.DeviceErrors,
                    };*/

               //Console.WriteLine("L.błedów " ,  data.DeviceErrors);


               var dataString = JsonConvert.SerializeObject(data);
               //Console.WriteLine(dataString);
               Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
               eventMessage.ContentType = MediaTypeNames.Application.Json;
               eventMessage.ContentEncoding = "utf-8";
               await client.SendEventAsync(eventMessage);
              


          }
          #endregion



          #region SendMessage

          public async Task SendMessage(dynamic data)
          {
             
               var twin = await client.GetTwinAsync();
              


          }

          #endregion


          #region SendMessageToIOT

          public async Task SendMessageToIOT(dynamic data)
          {
               var messageBody = JsonConvert.SerializeObject(data);  // konwersja z string na json
               Message message = new Message(Encoding.UTF8.GetBytes(messageBody));  //opbiekt wiadomosc zmienia sie na bajty w kodowaniu utf8

               // zmiana na typ kontentu jako json
               message.ContentType = MediaTypeNames.Application.Json;
               //kodowanie ustaic na utf8
               message.ContentEncoding = "utf-8";
               //wysłanie wiadomości 
               await client.SendEventAsync(message);
          }

          #endregion



          #region Device Twin

          public async Task UpdateTwinAsync()
          {
               var twin = await client.GetTwinAsync();

               Console.WriteLine($"\nInitial twin value received: \n{JsonConvert.SerializeObject(twin, Formatting.Indented)}");
               Console.WriteLine();

              // var reportedProperties = new TwinCollection();
              // reportedProperties["DateTimeLastAppLaunch"] = DateTime.Now;

             //  await client.UpdateReportedPropertiesAsync(reportedProperties);
          }

          private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
          {
               // wypisanie zmian które się odbyly w desiredProperties
               Console.WriteLine($"\tDesired property change:\n\t{JsonConvert.SerializeObject(desiredProperties)}");
               Console.WriteLine("\tSending current time as reported property");
               // utworzenie nowej kolekcji properties
               TwinCollection reportedProperties = new TwinCollection();
               // przypisanie nowej wartości do wskazanj propertki, tutaj akurat data bierząda 
               reportedProperties["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.Now;
               // updatujemy properties , ConfigureAwait(false) znaczy ze nie cgcemy wprowadzic konfiguracji
               await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
          }

          //reportet to co nam zwraca maszyna 
         // desired to to co my ustawiamy na maszynie

          public async Task UpdateTwinReported(string deviceName)
          {
               //   reportet to co nam zwraca maszyna 
               // W przypadku zmiany wartosci należy wysłać komunikat D2C ( device to cloud) do IOT
               // bieżaca wartosc musi byc w raportowanym bliżniaczym urządzeniu 
              // Console.WriteLine("urządzenie :", data.deviceName , "  błędy :" , data.DeviceErrorsValue);

               var twin = await client.GetTwinAsync();  // z UpdateTwinAsync()

               var odebraneWartosc = twin.Properties.Reported;    //reportet - wartosc na  maszynie
               var nowaWartosc = twin.Properties.Desired;         // desired - wartosc  ustawiamy na maszynie


               var name =  deviceName.Replace(" ", "");  // usuń spacje z nazwy 

                   //nazwy do JSON
               var device_error_count = deviceName + "_numer_bledu";
               var device_production_count = deviceName + "_production_procent";


               if(odebraneWartosc.Contains(deviceName )) // gdy w odebranejWartosci zawiera sie Device
               {
                    var zmienianaWartosc = odebraneWartosc[device_error_count];
                    try
                    {
                         await client.UpdateReportedPropertiesAsync(zmienianaWartosc);
                         Console.WriteLine("Zaaktualizowano pozycjeL  ", device_error_count);
                    }
                    catch (IotHubException ex)
                    {
                         Console.WriteLine("Bład aktualizacji pozycji ", device_error_count);
                    } 
                    

               }
               else
               {
                    Console.WriteLine(" Błą na urządzneiu się nei zmienił , nic nie zrobiono ");
               }
            




               // gdy nazwa w JSON istnieje czyli np"Device1_numer_bledu" 
              
              // reportedProperties[device_production_count] = data.ProductionRate;


             //  await client.UpdateReportedPropertiesAsync(reportedProperties);

          }


          #endregion Device Twin

           



     }
}
