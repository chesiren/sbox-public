namespace Sandbox;

/// <summary>
/// A search of the Workshop's games, a page at a time - from <see cref="MenuUtility.SearchWorkshopGames"/> - or its maps,
/// from <see cref="MenuUtility.SearchWorkshopMaps"/>. Just the ones that can be played here.
/// </summary>
public sealed class WorkshopGameSearch
{
	readonly Workshop.Query query;
	Workshop.QueryResult page;

	internal WorkshopGameSearch( Workshop.Query query )
	{
		this.query = query;
	}

	/// <summary>
	/// There's another page to ask for
	/// </summary>
	public bool HasMore { get; private set; } = true;

	/// <summary>
	/// The next page - none once there aren't any more, or the Workshop couldn't be asked
	/// </summary>
	public async Task<WorkshopItemDetails[]> NextAsync()
	{
		if ( !HasMore ) return [];

		// FORK-VR begin: in VR most pages are filtered empty, which would stop a list that loads more as it's scrolled
		for ( int tries = 0; ; tries++ )
		{
			page = page is null ? await query.RunAsync() : await page.GetNextResults();
			HasMore = page?.HasMoreResults() ?? false;

			var found = await MenuUtility.DetailsOfGamesAsync( page?.Items ?? [] );
			if ( found.Length > 0 || !HasMore || !Application.IsVR || tries >= 9 ) return found;
		}
		// FORK-VR end
	}
}
