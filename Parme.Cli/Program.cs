using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace Parme.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var codeGenCommand = new Command("codegen", "Generate code for emitter logic")
            {
                new Argument<string>("inputFile", "Input emitter logic definition file"),
                new Argument<string>("outputFile", "Name of the file to generate"),
                new Argument<string>("language", "language for the generated output"),
            };
            
            codeGenCommand.Handler = CommandHandler.Create<string, string, string, IConsole>(HandleCodeGen);
            var commands = new RootCommand
            {
                codeGenCommand,
            };

            commands.Invoke(args);
        }

        static void HandleCodeGen(string inputFile, string outputFile, string language, IConsole console)
        {
            if (!language.Trim().Equals("csharp"))
            {
                console.Error.Write("Invalid language value.  Valid values are: 'csharp'");
                return;
            }
            
            console.Out.Write($"inputFile: '{inputFile}'{Environment.NewLine}");
            console.Out.Write($"language: '{language}'{Environment.NewLine}");
            console.Out.Write($"outputFile: '{outputFile}'{Environment.NewLine}");
        }
    }
}