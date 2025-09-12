using Service.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(Services.ChargingService)))
            {
                try
                {
                    host.Open();
                    Console.WriteLine("Service is running...");
                    Console.ReadLine();
                    host.Close();
                    Console.WriteLine("Service has stopped.");
                    Console.ReadKey();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error starting service: {ex.Message}");
                    host.Abort();
                }
                
            }
            
        }
    }
}
