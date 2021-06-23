using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using TaggBot.Modules;

namespace TaggBot.Preconditions
{
    /// <summary>
    /// Precondition attribute that enforces a global cooldown per command.
    /// </summary>
    public class CommandCooldownAttribute : PreconditionAttribute
    {
        // Create a field to store the specified name
        private readonly double _cooldown;
        private static Dictionary<String, DateTime> globalCommandCooldown = new Dictionary<String, DateTime>(); // Dictionary to hold global command cooldowns for CommandCooldown attribute
        public override string ErrorMessage { get; set; }

        // Create a constructor so the name can be specified
        public CommandCooldownAttribute(double cooldown) => _cooldown = cooldown;

        // Override the CheckPermissions method
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (!globalCommandCooldown.ContainsKey(command.Name) || DateTime.UtcNow.Subtract(globalCommandCooldown[command.Name].ToUniversalTime()).TotalSeconds > _cooldown)
            {
                globalCommandCooldown[command.Name] = DateTime.UtcNow;
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("You're using that command too fast! Please wait " + string.Format("{0:0.00}", _cooldown - DateTime.UtcNow.Subtract(PublicModule.globalCommandCooldown[command.Name].ToUniversalTime()).TotalSeconds) + " second(s)."));
            }
        }
    }
}