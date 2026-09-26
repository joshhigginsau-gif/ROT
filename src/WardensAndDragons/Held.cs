namespace WardensAndDragons;

internal sealed class Held
{
	internal string Id = "";

	internal string Hero = "";

	internal string House = "";

	internal bool Hostage;

	internal int Taken;

	internal bool Forfeit;

	internal string Pack()
	{
		return string.Join("|", Hero, House, (!Hostage) ? "0" : "1", Taken.ToString(), (!Forfeit) ? "0" : "1");
	}

	internal static Held Unpack(string id, string s)
	{
		string[] array = (s ?? "").Split(new char[1] { '|' });
		if (array.Length < 5)
		{
			return null;
		}
		Held held = new Held();
		held.Id = id;
		held.Hero = array[0];
		held.House = array[1];
		held.Hostage = array[2] == "1";
		held.Taken = (int.TryParse(array[3], out var result) ? result : 0);
		held.Forfeit = array[4] == "1";
		return held;
	}
}
