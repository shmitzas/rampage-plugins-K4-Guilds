namespace K4_Guilds.Commands.Handlers;

public static class CreateHandler
{
	public static async Task<string> ExecuteAsync(CommandContext ctx)
	{
		if (ctx.Args.Length < 2)
			return ctx.Format("k4.command.usage.create");

		var (_, message, _) = await ctx.GuildService.CreateGuildAsync(ctx.Player, ctx.Args[0], ctx.Args[1]);
		return ctx.Format(message);
	}
}
