namespace Spookline.SPC.Console.Commands {
    public class RefreshHistoryCommand : Command {

        public override string Name => "refresh";
        public override string Description => "Refreshes the command history and commands.";

        public override CommandResult Execute(CommandContext context) {
            CommandSystem.Instance.Refresh();
            LogHistoryBuffer.Instance.SendRefreshHint();
            return CommandResult.Successful();
        }

    }
}