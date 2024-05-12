using Opc.UaFx.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuncionForBusinessLogic
{
     public class Class1
     {
         //private DeviceClient client;
          private OpcClient OPC;


          public Class1 ( OpcClient OPC)
          {
               
               this.OPC = OPC;
          }
     }
}

