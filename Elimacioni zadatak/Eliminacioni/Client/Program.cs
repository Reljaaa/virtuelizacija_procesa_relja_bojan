using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ChannelFactory<IStudentService> factory =
            new ChannelFactory<IStudentService>("StudentService");
            IStudentService proxy = factory.CreateChannel();

            while (true)
            {
                Console.Write("Unesite broj indeksa (ili 'exit' za kraj): ");
                string indeks = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(indeks) || indeks.ToLower() == "exit")
                    break;

                try
                {
                    var podaci = proxy.GetStudentInfo(indeks);
                    Console.WriteLine("\n--- Podaci o studentu ---");
                    Console.WriteLine(podaci);
                }
                catch (FaultException<StudentNotFoundException> ex)
                {
                    Console.WriteLine($"Greška: {ex.Detail.Message}");
                }

                Console.WriteLine();
            }
        }
    }
}
