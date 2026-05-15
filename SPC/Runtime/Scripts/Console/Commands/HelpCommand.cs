namespace Spookline.SPC.Console.Commands {
    public class HelpCommand : Command {

        public override string Name => "help";
        public override string Description => "Displays information about available commands.";

        private readonly Argument<string> _filter = Optional(
            "command",
            "The command to get help for. If not provided, lists all commands."
        ).String("");

        public HelpCommand() {
            Arguments(_filter);
        }

        public override CommandResult Execute(CommandContext context) {
            var text = CommandSystem.Instance.GetHelp(_filter[context]);
            return CommandResult.Successful(text);
        }

    }
}