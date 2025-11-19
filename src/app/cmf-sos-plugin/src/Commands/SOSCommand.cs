using System.CommandLine;

namespace Cmf.Sos.Plugin.Commands
{
    public static class SosCommand
    {
        public static Command Build()
        {
            var command = new Command("sos", "SOS Container troubleshooting commands");

            return command;
        }
    }
}