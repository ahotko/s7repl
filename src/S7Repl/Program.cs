using PrettyPrompt;
using PrettyPrompt.Completion;
using PrettyPrompt.Highlighting;
using S7ReplApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace S7Repl
{
    class Program
    {
        private static readonly (string name, string description, AnsiColor highlight)[] Commands = new[]
        {
            ( "connect", "Connects to S7 PLC; Usage: connect <ip address> [rack] [slot]", AnsiColor.Green ),
            ( "disconnect", "Disconnects from PLC.", AnsiColor.Green ),
            ( "read", "Read Data from address", AnsiColor.Green ),
            ( "read_struct", "a long curved fruit which grows in clusters and has soft pulpy flesh and yellow skin when ripe.", AnsiColor.Green ),
            ( "dump", "Dump a Byte array of data in DB of given address; Usage: dump <address> <offset> <length> [dec]", AnsiColor.Green ),
            ( "exit", "Exit S7REPL App.", AnsiColor.Green ),
        };

        private static readonly (string name, string description, AnsiColor highlight)[] DataTypes = new[]
        {
            ("Bit", "1 Bit (Logical)", AnsiColor.Yellow ),
            ("SInt", "8 Bit Signed Integer (-128...127)", AnsiColor.Yellow ),
            ("Int", "16 Bit SIgned Integer (-32.768...32.767)", AnsiColor.Yellow ),
            ("DInt", "32 Bit Signed Integer (-2.147.483.648...2.147.483.647)", AnsiColor.Yellow ),
            ("LInt", "64 Bit Signed Integer (-9.223.372.036.854.775.808...9.223.372.036.854.775.807)", AnsiColor.Yellow ),
            ("USInt", "8 Bit Unsigned Integer (0...255)", AnsiColor.Yellow ),
            ("UInt", "16 Bit Unsigned Integer (0...65.535)", AnsiColor.Yellow ),
            ("UDInt", "32 Bit Unsigned Integer (0...4.294.967.295)", AnsiColor.Yellow ),
            ("ULInt", "64 Bit Unsigned Integer (0...18.446.744.073.709.551.615)", AnsiColor.Yellow ),
            ("Byte", "Alias for USInt (8 Bit Unsigned Integer)", AnsiColor.Yellow ),
            ("Word", "Alias for UInt (16 Bit Unsigned Integer)", AnsiColor.Yellow ),
            ("DWord", "Alias for UDInt (32 Bit Unsigned Integer)", AnsiColor.Yellow ),
            ("LWord", "Alias for ULInt (64 Bit Unsigned Integer)", AnsiColor.Yellow ),
            ("Real", "32 Bit Signed Floating Point (single)", AnsiColor.Yellow ),
            ("LReal", "64 Bit Signed Floating Point (double)", AnsiColor.Yellow ),
            ("Datetime", "64 Bit BCD Encoded DateTime Value", AnsiColor.Yellow ),
            ("Date", "64 Bit BCD Encoded DateTime Value", AnsiColor.Yellow ),
            ("String", "String (Length must be supplied)", AnsiColor.Yellow ),
            ("ByteArray", "Array of Bytes (Length must be supplied)", AnsiColor.Yellow ),
        };

        private static Task<IReadOnlyList<CompletionItem>> FindCompletions(string typedInput, int caret)
        {
            var textUntilCaret = typedInput.Substring(0, caret);
            var previousWordStart = textUntilCaret.LastIndexOfAny(new[] { ' ', '\n', '.', '(', ')' });
            var typedWord = previousWordStart == -1
                ? textUntilCaret.ToLower()
                : textUntilCaret[(previousWordStart + 1)..].ToLower();

            return Task.FromResult<IReadOnlyList<CompletionItem>>(
                Commands
                .Where(command => command.name.ToLower().StartsWith(typedWord))
                .Select(command => new CompletionItem
                {
                    StartIndex = previousWordStart + 1,
                    ReplacementText = command.name,
                    DisplayText = command.name,
                    ExtendedDescription = new Lazy<Task<string>>(() => Task.FromResult(command.description))
                })
                .ToArray()
            );
        }

        // demo syntax highlighting callback
        private static Task<IReadOnlyCollection<FormatSpan>> Highlight(string text)
        {
            var spans = new List<FormatSpan>();

            for (int i = 0; i < text.Length; i++)
            {
                foreach (var command in Commands)
                {
                    if (text.Length >= i + command.name.Length && text.Substring(i, command.name.Length).ToLower() == command.name)
                    {
                        spans.Add(new FormatSpan(i, command.name.Length, new ConsoleFormat(Foreground: command.highlight)));
                        i += command.name.Length;
                        break;
                    }
                }
            }
            return Task.FromResult<IReadOnlyCollection<FormatSpan>>(spans);
        }

        static async Task Main(string[] args)
        {
            var prompt = new Prompt(callbacks: new PromptCallbacks()
            {
                CompletionCallback = FindCompletions,
                HighlightCallback = Highlight,
                //KeyPressCallbacks =
                //{
                //    [(ConsoleModifiers.Control, ConsoleKey.F1)] = ShowCommandDocumentation // could also just provide a ConsoleKey, instead of a tuple.
                //}
            });
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
