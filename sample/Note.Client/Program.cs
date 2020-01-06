using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Note.Contract;

namespace Note.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client1 = new Contract.Actor.ActorClient(channel);
            var reply = await client1.CreateActorAsync(
                new CreateActorCommand { Name = "actor" }
            );
            var client2 = new Contract.Note.NoteClient(channel);
            var reply1 = await client2.CreateNoteAsync(
                new CreateNoteCommand { Title = "title", Content = "initial content" }
            );
            var reply2 = await client2.ChangeNoteAsync(
                new ChangeNoteCommand { AggId = reply1.AggId, Content = "changed content" }
            );
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}