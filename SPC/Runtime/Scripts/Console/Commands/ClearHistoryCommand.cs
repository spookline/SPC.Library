namespace Spookline.SPC.Console.Commands {
    public class ClearHistoryCommand : Command {

        public override string Name => "clear";
        public override string Description => "Clears the command history.";

        public override CommandResult Execute(CommandContext context) {
            ConsoleHistoryBuffer.Instance.Clear();
            return CommandResult.Successful("History cleared");
        }

    }
}