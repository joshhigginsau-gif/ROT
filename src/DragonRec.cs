namespace WardensAndDragons
{
internal sealed class DragonRec
{
	internal string Id;

	internal string Name;

	internal string Item;

	internal string Rider = "";

	internal string Status = "riderless";

	internal int Born;

	internal int Temper = 50;

	internal int Kills;

	internal int LastTry = -99999;

	internal bool Alive => Status != "dead";

	internal bool Claimable => Status == "riderless" || Status == "wild";

	internal string Pack()
	{
		return string.Join("|", Name, Item, Born.ToString(), Temper.ToString(), Rider ?? "", Status, Kills.ToString(), LastTry.ToString());
	}

	internal static DragonRec Unpack(string id, string s)
	{
		string[] array = (s ?? "").Split('|');
		if (array.Length < 8)
		{
			return null;
		}
		DragonRec dragonRec = new DragonRec();
		dragonRec.Id = id;
		dragonRec.Name = array[0];
		dragonRec.Item = array[1];
		dragonRec.Born = (int.TryParse(array[2], out var result) ? result : 0);
		dragonRec.Temper = ((!int.TryParse(array[3], out result)) ? 50 : result);
		dragonRec.Rider = array[4];
		dragonRec.Status = array[5];
		dragonRec.Kills = (int.TryParse(array[6], out result) ? result : 0);
		dragonRec.LastTry = ((!int.TryParse(array[7], out result)) ? (-99999) : result);
		return dragonRec;
	}
}
}
