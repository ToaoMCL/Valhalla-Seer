using System;
using System.Collections.Generic;
using System.Text;

namespace Valhalla_Seer
{
    public class Program
    {
        static void Main(string[] args)
        {
            Bot bot = new Bot();

            bot.RunAsync().GetAwaiter().GetResult();
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("I'm out of here");
        }


    }
}

