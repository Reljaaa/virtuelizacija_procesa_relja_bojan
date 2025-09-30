using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace Service
{
    public class StudentService : IStudentService
    {
        private readonly string folderPath;

        public StudentService()
        {
            //možete staviti i putanju "C:\Studenti\Data" u App.config ukoliko vam je lakše
            //direktorijum "Data" se nalazi u bin\Debug
            string relativePath = System.Configuration.ConfigurationManager.AppSettings["StudentFilesPath"];
            folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
        }

        public string GetStudentInfo(string indeks)
        {
            string filePath = Path.Combine(folderPath, $"{indeks}.txt");

            if (!File.Exists(filePath))
            {
                throw new FaultException<StudentNotFoundException>(
                    new StudentNotFoundException($"Student sa indeksom {indeks} nije pronađen."),
                    "StudentNotFoundException");
            }

            return File.ReadAllText(filePath);
        }
    }
}
