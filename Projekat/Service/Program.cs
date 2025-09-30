using System;
using System.ServiceModel;
using Service.Domain;
using Service.Domain.Events;

namespace Service
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ConsoleUi.Init();
            var impl = new Service.Services.ChargingService();  
            using (ServiceHost host = new ServiceHost(impl))

                try
            {
                host.Open();
                Console.WriteLine("Service started. Press ENTER to stop.");
                Console.ReadLine();
                host.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                host.Abort();
            }
            // impl.Dispose() se poziva automatski zbog using-a
        }
    }
}



