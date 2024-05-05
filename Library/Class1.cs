using Newtonsoft.Json;
using Microsoft.Azure.Devices.Client;
using Opc.UaFx;
using Opc.UaFx.Client;
using System.Net.Mime;
using System.Text;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client.Exceptions;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Amqp.Framing;


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
          public async Task SendTelemetry(string DeviceName, object WorkorderId, object ProductionStatus, object Temperature, object ProductionRate,
                        object GoodCount, object BadCount, object DeviceErrors)
          {
               //Console.WriteLine(data);
               /*var selectedData = new
               {
                    DeviceName = DeviceName,
                    WorkorderId = WorkorderId,
                    ProductionStatus = ProductionStatus,
                    Temperature = Temperature,
                    ProductionRate = ProductionRate,
                    GoodCount = GoodCount,
                    BadCount = BadCount,
                    DeviceErrors = DeviceErrors,
               };*/

               var twin = await client.GetTwinAsync();
               var reportedProperties = twin.Properties.Reported;
               var nameDevice = DeviceName.Replace(" ", "");
               var device_error = nameDevice + "_numer_bledu";
               var errorStatus = DeviceErrors;
               bool DataNoChange = false;
               //DeviceError wysylamy tylko gdy sie zmini 

               if (reportedProperties.Contains(device_error))
               {
                    var currentError = reportedProperties[device_error];
                    DataNoChange = (currentError == errorStatus);
               }
               if (DataNoChange)
               {
                    // błąd się nei zmienił wiec nie wysyłamy wartosci error
                    var selectedData = new
                    {
                         DeviceName = DeviceName,
                         WorkorderId = WorkorderId,
                         ProductionStatus = ProductionStatus,
                         Temperature = Temperature,
                         ProductionRate = ProductionRate,
                         GoodCount = GoodCount,
                         BadCount = BadCount,

                    };
                    await SendMessageToIOT(selectedData);
                    Console.WriteLine(selectedData);
               }
               else
               {
                    var selectedData = new
                    {
                         DeviceName = DeviceName,
                         WorkorderId = WorkorderId,
                         ProductionStatus = ProductionStatus,
                         Temperature = Temperature,
                         ProductionRate = ProductionRate,
                         GoodCount = GoodCount,
                         BadCount = BadCount,
                         DeviceErrors = DeviceErrors,
                         // W przypadku zmiany wartść należy wysłać pojedyńczy komunikat D2C do platformy iOT ( punkt 2.7) 
                    };
                    Console.WriteLine(selectedData);
                    await SendMessageToIOT(selectedData);
               }

               await UpdateTwinAsync(nameDevice, errorStatus, ProductionRate);


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
          public async Task SetTwinAsync(string deviceName, int deviceError, int prodRate)
          {

               //DeviceError wysylamy tylko gdy sie zmini 

               // bliżniak jak chcemy zmienić jakąś konfiguracje  lub gdy urządzenie reportuje że zmienilo konfiguracje 
               var twin = await client.GetTwinAsync();
               Console.WriteLine($"\nInitial twin value received: \n{JsonConvert.SerializeObject(twin, Formatting.Indented)}");
               // formatowanier by bylo z ładnymi wcieciami
               string json = JsonConvert.SerializeObject(twin, Formatting.Indented);

               Console.WriteLine();
               var reportedProp = twin.Properties.Reported;    //reportet - wartosc na  maszynie
               var desiredProp = twin.Properties.Desired;         // desired - wartosc  ustawiamy na maszynie

               //object nameDevice = data.name; 
               var name = deviceName.Replace(" ", "");  // usuń spacje z nazwy 

               //nazwy do JSON
               var device_error = name + "_numer_bledu";
               var device_production = name + "_production_procent";
               // wartosci 
               var device_error_count = 0;
               var device_production_count = 0;


               if (reportedProp.Contains(device_error))
               {
                    var errorInThisMoment = reportedProp[device_error];
                    // jak błąd jest inny
                    if (errorInThisMoment != device_error_count)
                    {

                         // zmienił się błąd , trzeba wyświeylić informacje
                         var updateProp = new TwinCollection();
                         updateProp[device_error] = device_error_count;
                         try
                         {
                              await client.UpdateReportedPropertiesAsync(updateProp);
                              Console.WriteLine("Zaktualowano wlasnosc dla urzadzenia :   ", name, ".");
                              Console.WriteLine($"{DateTime.Now}> Device Twin   was update.");
                         }
                         catch (IotHubException ex)
                         {
                              Console.WriteLine("Blad podczas zmiany wartosci bledu", device_error);
                         }


                    }
                    else
                    {
                         Console.WriteLine(" Brak zmiany bledu - nie wykonano zmian");
                    }
               }
               if (desiredProp.Contains(device_production))
               {
                    //int device_production_count;
                    if (int.TryParse((string)desiredProp[device_production], out device_production_count))
                    {

                         OpcStatus tmp = OPC.WriteNode("ns=2;s=" + deviceName + "/ProductionRate", device_production_count);
                    }
               }



               //push update properties 
               // await client.UpdateReportedPropertiesAsync(reportedProperties);
               //Console.WriteLine($"{DateTime.Now}> Device Twin value was set.");

          }

          public async Task UpdateTwinAsync(string deviceName, object deviceError, object prodRate)
          {
               
               var twin = await client.GetTwinAsync();

               var reportedProp = twin.Properties.Reported;    //reportet - wartosc na  maszynie
               var desiredProp = twin.Properties.Desired;         // desired - wartosc  ustawiamy na maszynie

               //object nameDevice = data.name; 
               var name = deviceName.Replace(" ", "");  // usuń spacje z nazwy 

               //nazwy do JSON
               var device_error = name + "_numer_bledu";
               var device_production = name + "_production_procent";
               // wartosci 
               var device_error_count = deviceError;
               var device_production_count = prodRate;
               Console.WriteLine("error :", device_error, " , ", device_error_count);

               // Jeśli już taki wpis istnieje
               if (reportedProp.Contains(device_error))
               {
                    var errorInTgisMoment = reportedProp[device_error];
                    // jak błąd jest inny
                    if (errorInTgisMoment != device_error_count)
                    {

                         // zmienił się błąd , trzeba wyświeylić informacje
                         var updateProp = new TwinCollection();
                         updateProp[device_error] = device_error_count;
                         try
                         {
                              await client.UpdateReportedPropertiesAsync(updateProp);
                              Console.WriteLine("Zaktualowano liczbe bledów dla :   ", device_error, ".");
                              Console.WriteLine($"{DateTime.Now}> Device Twin   was update.");
                         }
                         catch (IotHubException ex)
                         {
                              Console.WriteLine("Blad podczas zmiany wartosci bledu", device_error);
                         }


                    }
                    else
                    {
                         Console.WriteLine(" Brak zmiany bledu - nie wykonano zmian");
                    }
               }
               else
               {
                    // jeśli nie ma takiego wpisu dla Device w reported to dodaj
                    var updateProp = new TwinCollection();
                    updateProp[device_error] = device_error_count;
                    try
                    {
                         await client.UpdateReportedPropertiesAsync(updateProp);
                         Console.WriteLine("Zaktualowano liczbe bledów dla :   ", device_error, ".");
                         Console.WriteLine($"{DateTime.Now}> Device Twin   was update.");
                    }
                    catch (IotHubException ex)
                    {
                         Console.WriteLine("Blad podczas zmiany wartosci bledu", device_error);
                    }
               }

               //  Console.WriteLine($"{DateTime.Now}> Device Twin value was update.");
               Console.WriteLine();

               // await client.UpdateReportedPropertiesAsync(reportedProp);
          }

          private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
          {
               //  Console.WriteLine($"\t{DateTime.Now}> Device Twin. Desired property change:\n\t{JsonConvert.SerializeObject(desiredProperties)}");
               //  string nodeId = (string)userContext;
               // int newProdRate = desiredProperties["ProductionRate"];
               // string node = nodeId + "/ProductionRate";

               TwinCollection reportedProperties = new TwinCollection();
               reportedProperties["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.Now;


               await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
          }
          #endregion Device Twin




          public async Task InitializeHandlers(string userContext)
          {


               //  await client.SetMethodHandlerAsync("SendMessages", SendMessageToIOT, client);


               await client.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, client);
               ;
          }


     }
}
