using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Spookline.SPC {
  public enum EntityType {

    Orc,
    Goblin,
    Dragon

  }

  public class SpawnCommand : Command {

    public override string Name => "spawn";
    public override string Description => "Spawns an entity at a location.";

    private readonly Argument<EntityType> _type = Argument(
      name: "type",
      description: "The type of entity to spawn"
    ).Enum<EntityType>();

    private readonly Argument<string> _name = Optional(
      name: "name",
      description: "The name of the entity to spawn"
    ).String("Unnamed");

    private readonly Argument<int> _amount = Optional(
      name: "amount",
      description: "Amount of entities to spawn"
    ).Int(1, min: 1);

    private readonly Argument<float> _scale = Optional(
      name: "scale",
      description: "Scale of the entity"
    ).Float(1f, min: 0.1f, max: 10f);

    private readonly Argument<Vector3> _position = Optional(
      name: "pos",
      description: "Position to spawn at"
    ).Vec3(Vector3.zero);

    private readonly Argument<List<EntityType>> _tags = Named(
      name: "tags",
      description: "A list of tags to apply to the entity"
    ).Enum<EntityType>().List();

    private readonly Argument<List<string>> _modifiers = Optional(
      name: "mod",
      description: "Modifiers for the entity"
    ).String().List();

    private readonly Argument<bool> _silent = Flag("silent", "If true, no message will be shown");

    public SpawnCommand() {
      Arguments(_type, _name, _amount, _scale, _position, _modifiers, _silent, _tags);
      Subcommands(new InfoSubcommand(), new ConfigSubcommand(), new TeleportSubcommand());
    }

    public override CommandResult Execute(CommandContext context) {
      var tags = _tags[context];
      var mods = _modifiers[context];

      var sb = new StringBuilder();
      sb.Append($"Spawning {_amount[context]}x {_type[context]} named '{_name[context]}'");
      sb.Append($" at {_position[context]} with scale {_scale[context]}");
      if (mods.Count > 0) sb.Append($" with modifiers: {string.Join(" and ", mods)}");
      if (tags.Count > 0) sb.Append($" and tags: {string.Join(" and ", tags)}");
      if (_silent[context]) sb.Append(" (silently)");

      return CommandResult.Successful(sb.ToString());
    }

    private class InfoSubcommand : Command {

      public override string Name => "info";
      public override string Description => "Shows info about spawning.";

      public override CommandResult Execute(CommandContext context) {
        return CommandResult.Successful("Spawn command allows you to create entities in the world.");
      }

    }

    private class ConfigSubcommand : Command {

      public override string Name => "config";
      public override string Description => "Configures spawn settings.";

      private readonly Argument<string> _key =
        Argument("key", "The config key").String();
      private readonly Argument<float> _speed =
        Named("speed", "A speed multiplier").Float(1f);

      private readonly Argument<bool> _verbose = Flag("verbose", "If true, more info will be shown");

      public ConfigSubcommand() {
        Arguments(_key, _speed, _verbose);
      }

      public override CommandResult Execute(CommandContext context) {
        var message = $"Config updated: {_key[context]} = " +
                      $"{_speed[context].ToString(CultureInfo.InvariantCulture)} " +
                      $"(Verbose: {_verbose[context]})";
        return CommandResult.Successful(message);
      }

    }

    private class TeleportSubcommand : Command {

      public override string Name => "tp";
      public override string Description => "Teleports to a location.";

      private readonly Argument<Vector3> _destination = Argument("dest", "Destination position").Vec3();
      private readonly Argument<Vector2> _rotation = Optional("rot", "Rotation (pitch,yaw)").Vec2();

      public TeleportSubcommand() {
        Arguments(_destination, _rotation);
      }

      public override async UniTask<CommandResult> ExecuteAsync(CommandContext context) {
        await UniTask.Delay(1000); // Simulate some async work
        return CommandResult.Successful($"Teleporting to {_destination[context]} with rotation {_rotation[context]}");
      }

    }

  }
}