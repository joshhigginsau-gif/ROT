using System;

namespace WardensAndDragons;

internal sealed class OathDef
{
	internal OathKind Kind;

	internal string Name;

	internal string Level;

	internal string Blurb;

	internal int LibertyPerYear;

	internal int FlatLiberty;

	internal int Burden;

	internal Func<int> TributePerFief;
}
