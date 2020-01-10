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
            var reply = await client1.CreateActorCommandAsync(
                new CreateActor { Name = "actor" }
            );
            var client2 = new Contract.Note.NoteClient(channel);
            var reply1 = await client2.CreateNoteCommandAsync(
                new CreateNote { Title = "title", Content = "initial content" }
            );
            var reply2 = await client2.ChangeNoteCommandAsync(
                new ChangeNote { AggId = reply1.AggId, Content = "changed content" }
            );
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}