using System.Collections.Concurrent;
using K4_Guilds.Database;
using K4_Guilds.Database.Models;

namespace K4_Guilds.Services;

public partial class GuildService(Plugin plugin, DatabaseService database)
{
	private const int CACHE_TTL_SECONDS = 300;

	private InviteService? _inviteService;

	private readonly ConcurrentDictionary<ulong, (Guild? Guild, DateTime ExpiresAt)> _playerGuildCache = new();
	private readonly ConcurrentDictionary<int, (Guild Guild, DateTime ExpiresAt)> _guildCache = new();

	public void SetInviteService(InviteService inviteService) => _inviteService = inviteService;

	public async Task<Guild?> GetGuildAsync(int guildId)
	{
		if (_guildCache.TryGetValue(guildId, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
			return cached.Guild;

		var guild = await database.GetGuildAsync(guildId);

		if (guild != null)
			_guildCache[guildId] = (guild, DateTime.UtcNow.AddSeconds(CACHE_TTL_SECONDS));
		else
			_guildCache.TryRemove(guildId, out _);

		return guild;
	}

	public async Task<Guild?> GetGuildByNameAsync(string name)
		=> await database.GetGuildByNameAsync(name);

	public async Task<Guild?> GetPlayerGuildAsync(ulong steamId)
	{
		if (_playerGuildCache.TryGetValue(steamId, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
			return cached.Guild;

		var guild = await database.GetPlayerGuildAsync(steamId);
		var expiry = DateTime.UtcNow.AddSeconds(CACHE_TTL_SECONDS);
		_playerGuildCache[steamId] = (guild, expiry);

		if (guild != null)
			_guildCache[guild.Id] = (guild, expiry);

		return guild;
	}

	public void InvalidatePlayerCache(ulong steamId)
		=> _playerGuildCache.TryRemove(steamId, out _);

	public void InvalidateGuildCache(int guildId)
	{
		_guildCache.TryRemove(guildId, out _);

		var toRemove = _playerGuildCache
			.Where(x => x.Value.Guild?.Id == guildId)
			.Select(x => x.Key)
			.ToArray();

		foreach (var key in toRemove)
			_playerGuildCache.TryRemove(key, out _);
	}

	public void InvalidateMemberCache(ulong steamId, int guildId)
	{
		InvalidatePlayerCache(steamId);
		InvalidateGuildCache(guildId);
	}

	public GuildMember? FindMember(Guild guild, ulong steamId)
		=> guild.Members.FirstOrDefault(m => m.SteamId == steamId);
}
