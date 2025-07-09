using Eliminator;
using Eliminator.Network.ProcessedPackets;
using EliminatorApp;
using System.Net.Sockets;

ClientGameManager? GetProgramInputs()
{
    bool? canContinue = null;
    var errorMessage = string.Empty;
    var idIsAssigned = false;

    void RequiresNewName(object? s, ProcessedConnectionResponsePacket? packet)
    {

        if (packet == null)
        {
            canContinue = false;
            errorMessage = "Corrupted packet received, please try again";
            return;
        }

        if (packet.Success)
        {
            canContinue = true;
            errorMessage = "Successful connection";
            return;
        }

        if (packet.Error is null)
        {
            canContinue = false;
            errorMessage = "Corrupted packet received, please try again";
            return;
        }

        canContinue = false;
        switch (packet.Error)
        {
            case ErrorMessage.UsernameTaken:
                errorMessage = "That name is already taken, please enter another";
                break;
            case ErrorMessage.GameIsFull:
                errorMessage = "The game is already full. Please try again later";
                break;
            default:
                errorMessage = "Unknown error has occurred";
                break;
        }
    }

    try
    {
        var dnsHostAddress = "*********";
        var dnsHostPort = 0000;
        while (true) // TODO: ERASE ABOVE VALUES and make it so we actually get user input
        {
            //Console.WriteLine("Enter the dns host address: ");
            //dnsHostAddress = Console.ReadLine();
            //Console.WriteLine("Enter the dns host port: ");
            //var dnsHostPortInput = Console.ReadLine();
            //Console.WriteLine("Enter your username: ");
            var r = new Random();
            var userName = Guid.NewGuid().ToString();

            //if (!int.TryParse(dnsHostPortInput, out dnsHostPort))
            //{
            //    Console.WriteLine("DNS host port entered incorrectly, expected an integer");
            //    continue;
            //}
            //else if (dnsHostAddress == null)
            //{
            //    Console.WriteLine("Can't enter empty argument for DNS host address");
            //    continue;
            //}
            //else if (userName == null)
            //{
            //    Console.WriteLine("Can't enter no username");
            //    continue;
            //}

            var cgm = new ClientGameManager(dnsHostAddress, dnsHostPort);

            cgm.BeginRun();
            cgm.AssignIdResponseEvent += (object? sender, ProcessedAssignIdPacket? packet) =>
            {
                idIsAssigned = true;
            };
            cgm.ConnectResponseEvent += RequiresNewName;

            for (var i = 0; i < 500; i++)
            {
                if (!idIsAssigned)
                {
                    Console.WriteLine("Waiting for id assignment...");
                    Thread.Sleep(100);
                }
                else
                {
                    Console.WriteLine("Id assigned! " + cgm.PlayerId);
                    break;
                }
            }

            if (!idIsAssigned)
            {
                Console.WriteLine("Timed out while awaiting id assignment. Closing program");
                cgm.Dispose();
                Environment.Exit(0);
            }

            cgm.SendConnectPacket(userName);
            while (canContinue == null)
            {
                Thread.Sleep(10);
            }

            if ((bool)canContinue)
            {
                Console.WriteLine("Beginning game...");
                Console.WriteLine($"Name: {cgm.Name}, Id: {cgm.PlayerId}");
                return cgm;
            }
            else
            {
                Console.WriteLine(errorMessage);
                continue;
            }
        }
    }
    catch (SocketException ex)
    {
        Console.WriteLine("Error occurred while attempting to server communication: " + ex.Message);
        return null;
    }
}

ProcessedInitialiseGamePacket? receivedInitialiseGame = null;
void BeginGame(object? sender, ProcessedInitialiseGamePacket? e)
{
    if (e is null)
    {
        Console.WriteLine("ProcessedInitialiseGamePacket was null, cannot continue");
        return;
    }
    else
    {
        receivedInitialiseGame = e;
    }
}

try
{
    ClientGameManager? cgm = GetProgramInputs();
    if (cgm is null)
    {
        Console.WriteLine("Cannot continue, see above messages");
        Environment.Exit(0);
    }

    cgm.InitialiseGameEvent += BeginGame;
    while (receivedInitialiseGame is null)
    {
        Console.WriteLine("Waiting to start game...");
        Thread.Sleep(100);
    }

    Console.WriteLine("Attempting to make game object");
    var game = new Game1(cgm, receivedInitialiseGame);
    Console.WriteLine("Attempting to run game object");
    game.Run();
}
catch (Exception ex)
{
    Console.WriteLine("Unknown exception occurred: " + ex.Message);
}
