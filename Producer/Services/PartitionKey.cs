using System;
using System.IO;

namespace Producer.Services
{
    public static class PartitionKey
    {
        private static readonly string[] Keys;
        private static readonly Random Rnd;

        static PartitionKey()
        {
            Keys = LoadKeys();
            Rnd = new Random();
        }

        public static string GetPartitionKey()
        {
            return Keys[Rnd.Next(Keys.Length)];
        }

        private static string[] LoadKeys()
        {
            var path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            return File.ReadAllLines(path + "\\addresses.csv");
        }
    }
}
