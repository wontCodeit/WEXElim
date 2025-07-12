using Eliminator;

var deck = new Deck(1);
var counter = new CardCounter();
var hm = new HandManager(2, 4, deck, counter);
//ServerTcp.InitialiseDns(0000);
var gm = new HostGameManager(hm, counter, ServerTcp.Instance);
gm.InitialiseAllClients(2);
Thread.Sleep(100); // Wait for the clients' games to start. TODO: refactor so this wait isn't necessary
gm.Run();
