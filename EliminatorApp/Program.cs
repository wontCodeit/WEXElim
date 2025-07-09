//using Eliminator;
//using Eliminator.Network.ProcessedPackets;
//using EliminatorApp;
//using System;
//using System.Net.Sockets;
//using System.Threading;

using EliminatorApp;
using System;

// TODO: Allow launch via EliminatorApp entrypoint
// =====================================================================================================================
Console.WriteLine("Launch via Eliminator.Client for now, if you aren't debugging!");
var igPacket = new Eliminator.Network.ProcessedPackets.ProcessedInitialiseGamePacket(255, 4, 1, 30, [(0, "W1"), (1, "W2"), (2, "W3")]);
var mockCGM = new MockClientGameManager("W1", 0, igPacket);
mockCGM.BeginRun();
Game1 game = new(mockCGM, igPacket);
mockCGM.MockOnly_ReceiveStartTurn();
game.Run(); // Nothing after this point is run until game is closed, it is a blocking call

// =====================================================================================================================

//ClientGameManager? GetProgramInputs()
//{
//    bool? canContinue = null;
//    var errorMessage = string.Empty;
//    var idIsAssigned = false;

//    void RequiresNewName(object? s, ProcessedConnectionResponsePacket? packet)
//    {

//        if (packet == null)
//        {
//            canContinue = false;
//            errorMessage = "Corrupted packet received, please try again";
//            return;
//        }

//        if (packet.Success)
//        {
//            canContinue = true;
//            errorMessage = "Successful connection";
//            return;
//        }

//        if (packet.Error is null)
//        {
//            canContinue = false;
//            errorMessage = "Corrupted packet received, please try again";
//            return;
//        }

//        canContinue = false;
//        switch (packet.Error)
//        {
//            case ErrorMessage.UsernameTaken:
//                errorMessage = "That name is already taken, please enter another";
//                break;
//            case ErrorMessage.GameIsFull:
//                errorMessage = "The game is already full. Please try again later";
//                break;
//            default:
//                errorMessage = "Unknown error has occurred";
//                break;
//        }
//    }

//    try
//    {
//        var dnsHostAddress = "";
//        var dnsHostPort = 0;
//        while (true)
//        {
//            Console.WriteLine("Enter the dns host address: ");
//            dnsHostAddress = Console.ReadLine();
//            Console.WriteLine("Enter the dns host port: ");
//            var dnsHostPortInput = Console.ReadLine();
//            Console.WriteLine("Enter your username: ");
//            var userName = Console.ReadLine();

//            if (!int.TryParse(dnsHostPortInput, out dnsHostPort))
//            {
//                Console.WriteLine("DNS host port entered incorrectly, expected an integer");
//                continue;
//            }
//            else if (dnsHostAddress == null)
//            {
//                Console.WriteLine("Can't enter empty argument for DNS host address");
//                continue;
//            }
//            else if (userName == null)
//            {
//                Console.WriteLine("Can't enter no username");
//                continue;
//            }

//            var cgm = new ClientGameManager(dnsHostAddress, dnsHostPort);

//            cgm.BeginRun();
//            cgm.AssignIdResponseEvent += (object? sender, ProcessedAssignIdPacket? packet) =>
//            {
//                idIsAssigned = true;
//            };
//            cgm.ConnectResponseEvent += RequiresNewName;

//            for (var i = 0; i < 500; i++)
//            {
//                if (!idIsAssigned)
//                {
//                    Console.WriteLine("Waiting for id assignment...");
//                    Thread.Sleep(10);
//                }
//                else
//                {
//                    Console.WriteLine("Id assigned!");
//                    break;
//                }
//            }

//            if (!idIsAssigned)
//            {
//                Console.WriteLine("Timed out while awaiting id assignment. Closing program");
//                cgm.Dispose();
//                Environment.Exit(0);
//            }

//            cgm.SendConnectPacket(userName);
//            while (canContinue == null)
//            {
//                Thread.Sleep(10);
//            }

//            if ((bool)canContinue)
//            {
//                Console.WriteLine("Beginning game...");
//                return cgm;
//            }
//            else
//            {
//                Console.WriteLine(errorMessage);
//                continue;
//            }
//        }
//    }
//    catch (SocketException ex)
//    {
//        Console.WriteLine("Error occurred while attempting to server communication: " + ex.Message);
//        return null;
//    }
//}

//void BeginGame(object? sender, ProcessedInitialiseGamePacket? e)
//{
//    if (e is null)
//    {
//        Console.WriteLine("ProcessedInitialiseGamePacket was null, cannot continue");
//        return;
//    }
//    // TODO: Let Game1 utilise TurnTimeLimit and Players list for display purposes

//    var game = new Game1(sender as ClientGameManager, e);
//    game.Run();
//}

//try
//{
//    ClientGameManager? cgm = GetProgramInputs();
//    if (cgm is null)
//    {
//        Console.WriteLine("Cannot continue, see above messages");
//        Environment.Exit(0);
//    }

//    cgm.InitialiseGameEvent += BeginGame;
//}
//catch (Exception ex)
//{
//    Console.WriteLine("Unknown exception occurred: " + ex.Message);
//}
