using PrettyPrompt;
using S7ReplApp;
using System;
using System.Threading.Tasks;

namespace S7Repl
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var prompt = new Prompt();
            var dumper = new S7Worker();

            while (true)
            {
                var response = await prompt.ReadLineAsync("> ");
                if (response.IsSuccess)
                {
                    if (response.Text == "exit") break;

                    try
                    {
                        var command = Parser.ParseCommand(response.Text);

                        if (command.Command.Equals("connect"))
                        {
                            var parameters = Parser.ParseConnectParameters(command.Params);
                            dumper.Connect(parameters.IpAddress, parameters.Rack, parameters.Slot);

                            Console.WriteLine($"Connected to {parameters.IpAddress}, rack {parameters.Rack}, slot {parameters.Slot}");
                        }

                        if (command.Command.Equals("disconnect"))
                        {
                            dumper.Disconnect();

                            Console.WriteLine($"Disconnected from S7 PLC");
                        }

                        if (command.Command.Equals("dump"))
                        {
                            var parameters = Parser.ParseDumpParameters(command.Params);

                            dumper.Dump(parameters.DataBlock, parameters.Offset, parameters.Length, parameters.UseDecimal);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR: {ex.Message}");
                    }


                }
            }

            dumper.Disconnect();
        }
    }
}
