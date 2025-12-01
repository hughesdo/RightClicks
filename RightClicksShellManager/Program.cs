using System;

namespace RightClicksShellManager
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("RightClicks Shell Manager");
            Console.WriteLine("Usage: RightClicksShellManager.exe /install | /uninstall");
            
            if (args.Length == 0)
            {
                Console.WriteLine("No arguments provided.");
                return;
            }
            
            string command = args[0].ToLower();
            
            switch (command)
            {
                case "/install":
                    Console.WriteLine("Installing shell hooks...");
                    // TODO: Implement shell hook installation
                    break;
                    
                case "/uninstall":
                    Console.WriteLine("Uninstalling shell hooks...");
                    // TODO: Implement shell hook uninstallation
                    break;
                    
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    break;
            }
        }
    }
}

