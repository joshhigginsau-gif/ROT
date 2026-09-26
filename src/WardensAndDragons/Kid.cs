namespace WardensAndDragons;

internal sealed class Kid
{
	internal string Id = "";

	internal string Hero = "";

	internal string Other = "";

	internal string Where = "";

	internal int Night;

	internal string Parent = "";

	internal bool Known;

	internal bool Legit;

	internal string Pack()
	{
		return string.Join("|", Hero, Other, Where, Night.ToString(), (!Known) ? "0" : "1", (!Legit) ? "0" : "1", Parent);
	}

	internal static Kid Unpack(string id, string s)
	{
		string[] array = (s ?? "").Split(new char[1] { '|' });
		if (array.Length < 6)
		{
			return null;
		}
		Kid kid = new Kid();
		kid.Id = id;
		kid.Hero = array[0];
		kid.Other = array[1];
		kid.Where = array[2];
		kid.Night = (int.TryParse(array[3], out var result) ? result : 0);
		kid.Known = array[4] == "1";
		kid.Legit = array[5] == "1";
		kid.Parent = ((array.Length <= 6) ? "" : array[6]);
		return kid;
	}
}
