using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons;

internal static class Flow
{
	internal static void Begin(Hero hero)
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Expected O, but got Unknown
		try
		{
			if (hero == null || hero.Clan == null)
			{
				return;
			}
			Clan recipient = hero.Clan;
			List<KeyValuePair<string, object>> list = Bellum.TitlesHeldBy(Clan.PlayerClan);
			if (list.Count == 0)
			{
				Notify("You hold no realm to grant.");
				return;
			}
			List<InquiryElement> list2 = new List<InquiryElement>();
			foreach (KeyValuePair<string, object> item in list)
			{
				list2.Add(new InquiryElement(item.Value, item.Key, (ImageIdentifier)null));
			}
			Inquiry.Select("Name a Warden", "Choose the realm " + N(recipient) + " shall hold in your name.", list2, 1, 1, "Grant", "Cancel", delegate(List<InquiryElement> chosen)
			{
				if (chosen != null && chosen.Count != 0)
				{
					object identifier = chosen[0].Identifier;
					if (!Bellum.Grant(identifier, recipient, out var reason))
					{
						Notify("The grant failed: " + (reason ?? "no reason given"));
						Log.Write("grant refused: " + reason);
					}
					else
					{
						Notify(N(recipient) + " now holds " + Bellum.PlainName(identifier) + " in your name.");
						Log.Write("granted " + Bellum.PlainName(identifier) + " to " + N(recipient));
						StepStyle(recipient, identifier);
					}
				}
			});
		}
		catch (Exception ex)
		{
			Log.Write("Begin failed: " + ex.Message);
		}
	}

	private static void StepStyle(Clan warden, object title)
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		try
		{
			string canon = Styles.CanonicalFor(Bellum.PlainName(title));
			List<InquiryElement> list = new List<InquiryElement>();
			if (canon != null)
			{
				list.Add(new InquiryElement((object)"canon", canon, (ImageIdentifier)null));
			}
			list.Add(new InquiryElement((object)"custom", "A style of your own choosing...", (ImageIdentifier)null));
			list.Add(new InquiryElement((object)"none", "No style - simply lord of their lands", (ImageIdentifier)null));
			Inquiry.Select("The Style", "How shall " + N(warden) + " be known as holder of " + Bellum.PlainName(title) + "?" + ((canon != null) ? (" The realm's traditional title is " + canon + ".") : ""), list, 1, 1, "Proclaim", "Skip", delegate(List<InquiryElement> chosen)
			{
				string text3 = ((chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as string) : "none");
				if (text3 == "canon")
				{
					Styles.Set(warden, canon);
					Notify(N(warden) + " is proclaimed " + canon + ".");
					StepRename(warden, title);
				}
				else if (text3 == "custom")
				{
					Inquiry.Text("The Style", "Name the style " + N(warden) + " shall bear.", canon ?? "Warden of ", "Proclaim", "Cancel", delegate(string text2)
					{
						if (!string.IsNullOrEmpty(text2))
						{
							Styles.Set(warden, text2.Trim());
							Notify(N(warden) + " is proclaimed " + text2.Trim() + ".");
						}
						StepRename(warden, title);
					}, delegate
					{
						StepRename(warden, title);
					});
				}
				else
				{
					StepRename(warden, title);
				}
			}, delegate
			{
				StepRename(warden, title);
			});
		}
		catch (Exception ex)
		{
			Log.Write("style step failed: " + ex.Message);
			StepRename(warden, title);
		}
	}

	private static void StepRename(Clan warden, object title)
	{
		if (!Cfg.StepRename || !Bellum.Init())
		{
			StepOath(warden, title);
			return;
		}
		try
		{
			string current = Bellum.PlainName(title);
			Inquiry.Text("Name the Realm", "By what name shall this wardenship be known? Leave it as it stands to keep " + current + ".", current, "Proclaim", "Keep", delegate(string text)
			{
				if (!string.IsNullOrEmpty(text) && text != current)
				{
					if (Bellum.Rename(title, text, out var reason))
					{
						Notify("It shall be known as " + text + ".");
						Log.Write("renamed to '" + text + "'");
					}
					else
					{
						Notify("The name was refused: " + (reason ?? "no reason given"));
						Log.Write("rename refused: " + reason);
					}
				}
				StepOath(warden, title);
			}, delegate
			{
				StepOath(warden, title);
			});
		}
		catch (Exception ex)
		{
			Log.Write("rename step failed: " + ex.Message);
			StepOath(warden, title);
		}
	}

	private static void StepOath(Clan warden, object title)
	{
		if (!Cfg.StepOath)
		{
			StepVassals(warden, title);
			return;
		}
		try
		{
			Oaths.Choose("Terms of the Oath", "On what terms does " + N(warden) + " hold this realm? Lighter oaths buy loyalty; heavier ones fill your coffers and breed resentment.", warden, null, delegate(OathKind kind)
			{
				if (kind != 0)
				{
					Notify(Oaths.SetForClan(warden, kind, quiet: true));
				}
				StepVassals(warden, title);
			});
		}
		catch (Exception ex)
		{
			Log.Write("oath step failed: " + ex.Message);
			StepVassals(warden, title);
		}
	}

	private static void StepVassals(Clan warden, object title)
	{
		if (Cfg.StepVassals)
		{
			SwearHouses(warden, title, null);
		}
	}

	internal static void SwearHouses(Clan warden, object title, Action after)
	{
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Expected O, but got Unknown
		try
		{
			Clan playerClan = Clan.PlayerClan;
			Kingdom val = ((playerClan != null) ? playerClan.Kingdom : null);
			if (val == null || warden == null || title == null)
			{
				if (after != null)
				{
					after();
				}
				return;
			}
			int tier = Bellum.TierOf(title);
			List<InquiryElement> list = new List<InquiryElement>();
			foreach (Clan item in (List<Clan>)(object)val.Clans)
			{
				if (item != null && !item.IsEliminated && item != playerClan && item != warden)
				{
					List<KeyValuePair<string, object>> list2 = Bellum.TitlesHeldBy(item);
					int num = list2.Count((KeyValuePair<string, object> t) => Bellum.TierOf(t.Value) < tier && Bellum.HoldsDirectlyOfPlayer(t.Value) && Bellum.IdOf(t.Value) != Bellum.IdOf(title));
					string text = ((list2.Count == 0) ? "holds no land" : ((num == 0) ? "holds nothing of you that this wardenship outranks" : (num + " title(s) can go beneath it")));
					list.Add(new InquiryElement((object)item, string.Concat(item.Name, "  (", text, ")"), (ImageIdentifier)null, num > 0, text));
				}
			}
			if (list.Count == 0)
			{
				if (after != null)
				{
					after();
				}
				return;
			}
			Inquiry.Select("Swear the Houses", "Which houses shall hold of " + Styles.Titled(warden) + " rather than of you? Their lands will be placed beneath " + Bellum.PlainName(title) + ". Only lands ranking below it can go there.", list, 0, list.Count, "Swear them", "Skip", delegate(List<InquiryElement> chosen)
			{
				//IL_0075: Unknown result type (might be due to invalid IL or missing references)
				//IL_007c: Expected O, but got Unknown
				if (chosen == null || chosen.Count == 0)
				{
					if (after != null)
					{
						after();
					}
				}
				else
				{
					List<string> list3 = new List<string>();
					List<string> list4 = new List<string>();
					int num2 = 0;
					foreach (InquiryElement item2 in chosen)
					{
						object identifier = item2.Identifier;
						Clan val2 = (Clan)((!(identifier is Clan)) ? null : identifier);
						if (val2 != null)
						{
							int num3 = 0;
							string text2 = null;
							foreach (KeyValuePair<string, object> item3 in Bellum.TitlesHeldBy(val2))
							{
								if (!(Bellum.IdOf(item3.Value) == Bellum.IdOf(title)) && Bellum.HoldsDirectlyOfPlayer(item3.Value))
								{
									if (Bellum.PlaceBeneath(item3.Value, title, out var why))
									{
										num3++;
									}
									else
									{
										text2 = why;
									}
								}
							}
							if (num3 > 0)
							{
								num2 += num3;
								list3.Add(N(val2));
								Store.Set("sworn:" + ((MBObjectBase)val2).StringId, ((MBObjectBase)warden).StringId);
								Store.Set("swornt:" + ((MBObjectBase)val2).StringId, Bellum.IdOf(title));
								Log.Write("placed " + num3 + " title(s) of " + N(val2) + " beneath " + Bellum.PlainName(title));
							}
							else
							{
								list4.Add(N(val2) + " (" + (text2 ?? "nothing to place") + ")");
							}
						}
					}
					if (num2 > 0)
					{
						Bellum.RebuildIndexes();
						Store.AddDeed(Standing.Date() + "  " + string.Join(", ", list3.ToArray()) + " swore to " + Styles.Titled(warden) + ".");
					}
					Notify(((list3.Count > 0) ? (string.Join(", ", list3.ToArray()) + " now hold of " + Styles.Titled(warden) + ".") : "No house was sworn.") + ((list4.Count > 0) ? (" Could not: " + string.Join("; ", list4.ToArray()) + ".") : ""));
					if (after != null)
					{
						after();
					}
				}
			}, delegate
			{
				if (after != null)
				{
					after();
				}
			});
		}
		catch (Exception ex)
		{
			Log.Write("swearing houses failed: " + ex);
			if (after != null)
			{
				after();
			}
		}
	}

	internal static void BeginRevoke(Hero hero)
	{
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected O, but got Unknown
		try
		{
			if (hero == null || hero.Clan == null)
			{
				return;
			}
			Clan holder = hero.Clan;
			List<KeyValuePair<string, object>> list = Bellum.TitlesHeldBy(holder);
			if (list.Count == 0)
			{
				Notify("They hold nothing you may take.");
				return;
			}
			List<InquiryElement> list2 = new List<InquiryElement>();
			foreach (KeyValuePair<string, object> item in list)
			{
				list2.Add(new InquiryElement(item.Value, item.Key, (ImageIdentifier)null));
			}
			Inquiry.Select("Strip a Warden", "Which realm do you take back from " + N(holder) + "? They will not forget it.", list2, 1, 1, "Revoke", "Relent", delegate(List<InquiryElement> chosen)
			{
				if (chosen != null && chosen.Count != 0)
				{
					if (Bellum.Revoke(chosen[0].Identifier, out var reason))
					{
						Notify("You have stripped " + N(holder) + " of " + Bellum.PlainName(chosen[0].Identifier) + ".");
						Log.Write("revoked from " + N(holder));
						Standing.Change(-Cfg.RevokeHonour, Cfg.RevokeDread, "Revoked the title of " + N(holder));
						Styles.Set(holder, null);
					}
					else
					{
						Notify("The revocation failed: " + (reason ?? "no reason given"));
						Log.Write("revoke refused: " + reason);
					}
				}
			});
		}
		catch (Exception ex)
		{
			Log.Write("revoke failed: " + ex.Message);
		}
	}

	private static string N(Clan c)
	{
		try
		{
			return (c == null) ? "that house" : ((object)c.Name).ToString();
		}
		catch
		{
			return "that house";
		}
	}

	internal static void Notify(string msg)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(msg));
		}
		catch
		{
		}
		Log.Write(msg);
	}
}
