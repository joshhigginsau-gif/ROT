using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// The Bastard's Banner.
	//
	// A ruler's death used to be a popup that told you how a Great Council you
	// had never held had gone. This is the replacement, and it is the opposite
	// shape: nothing is tracked, nothing accumulates, nothing happens unless
	// you say so. You die, you are asked one question, and if you say yes the
	// map changes that afternoon.
	//
	// What happens is Blackfyre. A child you fathered and never acknowledged
	// walks out of your own history with your blood, your sigil in reversed
	// colours, one of your castles, a third of your sworn houses and a war.
	// Then you choose which of them you are.
	internal static class Bastard
	{
		private const string RisenKey = "bs:risen";

		private const string HeadKey = "bs:head";

		private const string HouseKey = "bs:house";

		private const string RealmKey = "bs:realm";

		internal static bool Risen
		{
			get
			{
				return Store.Get(RisenKey) == "1";
			}
		}

		internal static Hero Head
		{
			get
			{
				return Find(Store.Get(HeadKey));
			}
		}

		internal static Clan House
		{
			get
			{
				try
				{
					string id = Store.Get(HouseKey);
					return string.IsNullOrEmpty(id)
						? null
						: Clan.All.FirstOrDefault((Clan c) => ((MBObjectBase)c).StringId == id && !c.IsEliminated);
				}
				catch
				{
					return null;
				}
			}
		}

		private static Hero Find(string id)
		{
			try
			{
				return string.IsNullOrEmpty(id)
					? null
					: Hero.AllAliveHeroes.FirstOrDefault((Hero h) => ((MBObjectBase)h).StringId == id);
			}
			catch
			{
				return null;
			}
		}

		// ------------------------------------------------------------------
		// the question

		// Asked once, when the ruler of the player's house dies, and never
		// again. Decline and the campaign carries on exactly as it would have.
		internal static void Offer(Hero dead)
		{
			try
			{
				// Asked at most once. Note the test is on the key being SET,
				// not on Risen - "declined" is an answer too, and testing
				// Risen alone meant refusing the bastard asked you again on
				// the next death, which is the opposite of what both this
				// comment and the config promised.
				if (dead == null)
				{
					return;
				}
				string why = WhyNot();
				if (why != null)
				{
					Log.Write("the bastard was not offered: " + why);
					return;
				}
				Hero heir = Hero.MainHero;

				// Is there a real child out there, and what did you do about
				// them? This is the whole spectrum: the stranger at the gate
				// is now only what happens when you never had one.
				Blade.Reckoning how;
				Hero mine2 = Blade.Claimant(out how);
				if (mine2 != null)
				{
					Reckon(dead, heir, mine2, how);
					return;
				}
				if (!Cfg.BastardStranger)
				{
					Log.Write("no child of yours is out there, and the stranger is turned off");
					return;
				}
				string line =
					"A man came to the gate during the funeral and would not give his name to the guards.\n\n" +
					"He carried a token " + ((dead.IsFemale) ? "your mother" : "your father") +
					" is said to have given away a long time ago, in a part of " +
					((dead.BornSettlement != null) ? dead.BornSettlement.Name.ToString() : "the realm") +
					" nobody in this house talks about. He has the look. Everyone who saw him says so, and then says they did not.\n\n" +
					"He is not asking to be acknowledged. He has already gone, and men have gone with him.\n\n" +
					"Somewhere out there a banner is being sewn in your colours, the wrong way round.";
				Inquiry.Confirm("A Face You Know", line, "Let it come", "There was no man at the gate",
					delegate
					{
						Rise(dead, heir);
					},
					delegate
					{
						Store.Set(RisenKey, "declined");
						Log.Write("the bastard was turned away at the gate");
					});
			}
			catch (Exception e)
			{
				Log.Write("offering the bastard failed: " + e.Message);
			}
		}

		// Why the question would not be asked today, or null if it would.
		//
		// The same gates Offer has always had, pulled out so wad.bastard can
		// say which one is closed instead of the player dying to find out.
		internal static string WhyNot()
		{
			try
			{
				if (!Cfg.Bastard)
				{
					return "bastards_banner is off in the config";
				}
				string state = Store.Get(RisenKey);
				if (!string.IsNullOrEmpty(state))
				{
					return "it has already been answered in this campaign (" + state + ")";
				}
				Clan mine = Clan.PlayerClan;
				if (mine == null || mine.Kingdom == null)
				{
					return "your house is in no realm";
				}
				// And only to a house that actually rules. A sworn vassal's
				// death has no realm to divide: the vassals, the castles and
				// the war would all belong to his liege, and splitting them
				// would hand a third of somebody else's kingdom - possibly
				// including their king - to a stranger.
				if (!Succession.Rules())
				{
					return "your house rules nothing to divide";
				}
				// Nothing to split. A house with one holding would be handing
				// over its only seat, and a realm with no vassals has nobody
				// to take.
				if (Fiefs().Count < Cfg.BastardMinFiefs)
				{
					return "your house holds " + Fiefs().Count + " town(s) or castle(s), and bastard_min_fiefs is " + Cfg.BastardMinFiefs;
				}
				Blade.Reckoning how;
				if (Blade.Claimant(out how) == null && !Cfg.BastardStranger)
				{
					return "no child of yours is out there, and bastard_stranger is off";
				}
				return null;
			}
			catch (Exception e)
			{
				return "it cannot be read just now: " + e.Message;
			}
		}

		// Forget the answer, so the next death asks again. For testing only:
		// whatever was raised last time stays raised.
		internal static void ForgetAnswer()
		{
			Store.Set(RisenKey, null);
			Store.Set(HeadKey, null);
			Store.Set(HouseKey, null);
			Store.Set(RealmKey, null);
		}

		// Raise it now, as if the ruler had died this morning. For testing.
		internal static void OfferNow()
		{
			Offer(Hero.MainHero);
		}

		// A child of yours, and the reckoning for how you treated them.
		//
		// This is the case the whole system was rebuilt for. There is no
		// stranger to explain: you know exactly who this is, you know what you
		// gave them, and the size of what happens next is the size of what you
		// gave.
		private static void Reckon(Hero dead, Hero heir, Hero him, Blade.Reckoning how)
		{
			try
			{
				string blade = Lore.Blade();
				bool armed = (how == Blade.Reckoning.Armed || how == Blade.Reckoning.Both);
				bool named = (how == Blade.Reckoning.Acknowledged || how == Blade.Reckoning.Both);

				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				sb.Append(him.Name).Append(" has not come to the funeral.\n\n");
				sb.Append(named
					? ("You wrote them into the book yourself. They have your name, and every lord who knelt at that ceremony remembers doing it.\n\n")
					: ("They were never written into the book. That has not stopped anyone from counting on their fingers.\n\n"));
				if (armed)
				{
					sb.Append("And they are carrying ").Append(blade)
					  .Append(". You put it in their hand. Whatever anybody says about their birth, nobody can say you did not choose them.\n\n");
				}
				int wins = Tourney.Wins(him);
				if (wins > 0)
				{
					sb.Append("And the smallfolk know that face. They watched it win ")
					  .Append((wins == 1) ? "a tourney" : (wins + " tourneys"))
					  .Append(Tourney.Crowned(him)
						? ", and a crowd that has cheered a man that often will follow him a long way.\n\n"
						: ", and they have not forgotten.\n\n");
				}
				sb.Append("Men have been riding to them since the day you died.\n\n");
				sb.Append(armed
					? "This will be a war."
					: "How far it goes is anyone's guess.");

				Inquiry.Confirm("A Child of Yours", sb.ToString(),
					"Let it come", "They would not dare",
					delegate
					{
						Rise(dead, heir, him, how);
					},
					delegate
					{
						Store.Set(RisenKey, "declined");
						Log.Write("the reckoning was waved off");
					});
			}
			catch (Exception e)
			{
				Log.Write("the reckoning failed: " + e.Message);
			}
		}

		// ------------------------------------------------------------------
		// the rising

		private static void Rise(Hero dead, Hero heir)
		{
			Rise(dead, heir, null, Blade.Reckoning.None);
		}

		private static void Rise(Hero dead, Hero heir, Hero already, Blade.Reckoning how)
		{
			try
			{
				// The belt to Blade.Impossible's braces. Whatever route got us
				// here, the claimant is never the person holding the seat.
				if (already != null && (already == heir || already == Hero.MainHero
					|| (Clan.PlayerClan != null && Clan.PlayerClan.Leader == already)))
				{
					Log.Write("the rising was stopped: the claimant is the one on the seat");
					Store.Set(RisenKey, null);
					Flow.Notify("There is nobody to raise a banner against you. The claim is yours, and you are already holding it.");
					return;
				}
				Clan mine = Clan.PlayerClan;
				Kingdom yours = (mine != null) ? mine.Kingdom : null;
				if (mine == null || yours == null)
				{
					return;
				}

				// 1. The fief they take. At random, from what your house holds
				//    and is not already spoken for as somebody's seat.
				Settlement seat = Pick(Fiefs());
				if (seat == null)
				{
					Log.Write("the bastard rose and there was nothing to give them");
					return;
				}

				// Mark it now, not at the end. Everything below registers
				// objects with the campaign the moment it is called, so a
				// throw halfway leaves a hero or a clan in the save - and if
				// the flag were only written on success, the next death would
				// run the whole thing again and leave another pair.
				// Marked as attempted, not as risen. Everything below
				// registers objects with the campaign the moment it is called,
				// so a throw halfway must not leave the door open for a second
				// attempt on the next death - but nor should a failure report
				// a banner that was never actually raised.
				Store.Set(RisenKey, "failed");

				// 2. The claimant. A child you already have, if there is one -
				//    they were made years ago and have been living their life
				//    since. Only invent somebody when there is nobody.
				Hero him = already ?? Sire(dead, seat);
				if (him == null)
				{
					Log.Write("the bastard could not be born");
					return;
				}

				// 3. Their house: your sigil, your colours reversed.
				Clan house = Raise(him, seat, mine, Lore.Blade());
				if (house == null)
				{
					// Do not leave him wandering the save with no clan and no
					// story. If he cannot have a house he was never born.
					Log.Write("the bastard has no house to his name");
					// Only a claimant we invented a moment ago. A child of
					// yours has existed for years, may be in your clan, may be
					// your named heir - deleting them because a clan could not
					// be created would be far worse than the failure itself.
					if (already == null)
					{
						Unmake(him);
					}
					return;
				}
				Store.Set(RisenKey, "1");

				// And he takes the sword's name for his own. He was born a
				// Waters or a Snow, by where he was got; from today he is the
				// blade, which is exactly what Daemon Waters did when he
				// became Daemon Blackfyre.
				//
				// Only a child you WROTE INTO THE BOOK takes the sword's name.
				//
				// That is the historical order and it settles a real problem.
				// Daemon was legitimised by Aegon IV and only then became
				// Daemon Blackfyre. A child you armed but never acknowledged
				// is still Jon Snow, and he should stay Jon Snow - both
				// because his whole claim rests on being your baseborn son,
				// and because renaming him off a bastard surname would make
				// RoT report him as trueborn, which is the one thing he
				// demonstrably is not.
				if (already == null || how == Blade.Reckoning.Both)
				{
					Rename(him);
				}

				// Only now does he become the dead ruler's child. A hero's
				// children list is append-only - there is no way to take a
				// name back out of it - so claiming him before the house was
				// certain would have left a phantom dead half-sibling in your
				// heir's family for the rest of the campaign.
				if (already == null)
				{
					Claim(him, dead);
				}

				// 4. The castle actually changes hands. Until now he was in
				//    your clan and a grant would have moved nothing.
				try
				{
					ChangeOwnerOfSettlementAction.ApplyByGift(seat, him);
				}
				catch (Exception se)
				{
					Log.Write("the seat would not go with him: " + se.Message);
				}

				// 5. A kingdom, but only for a claim worth the name. A child
				//    you ignored and never armed takes a castle and whatever
				//    banners follow, and that is all.
				//    A champion of the lists is the exception: a child the
				//    crowds have cheered often enough has a following no book
				//    gave them.
				bool champion = already != null && Tourney.Crowned(already);
				Kingdom realm = (already != null && !Blade.Crowns(how) && !champion) ? null : Crown(house, him, seat);
				if (realm == null)
				{
					// Stop here. A clan cannot be joined, so without a kingdom
					// the defections below would not move houses TO anybody -
					// they would just make a third of your realm independent,
					// which costs the player everything and gains the bastard
					// nothing. He keeps the castle and the claim instead.
					Log.Write("no kingdom was founded; the bastard keeps " + seat.Name + " and nothing else");
					Store.Set(HeadKey, ((MBObjectBase)him).StringId);
					Store.Set(HouseKey, ((MBObjectBase)house).StringId);
					Store.AddDeed(Standing.Date() + "  " + him.Name + " took " + seat.Name + " and held it.");
					return;
				}

				// 6. And the houses that go with them, as many as you earned.
				//    Every tourney they won brings more of them.
				float share = Math.Min(0.9f, Blade.Share(how) + Tourney.ShareBonus(him));
				int gone = Defect(yours, realm, house, share);

				// 7. And a war, if the claim is one that can carry a war.
				if ((already == null) ? Cfg.BastardWar : (Blade.Declares(how) || (champion && Cfg.BastardWar)))
				{
					try
					{
						DeclareWarAction.ApplyByDefault(realm, yours);
					}
					catch (Exception we)
					{
						Log.Write("the war would not be declared: " + we.Message);
					}
				}

				Store.Set(HeadKey, ((MBObjectBase)him).StringId);
				Store.Set(HouseKey, ((MBObjectBase)house).StringId);
				Store.Set(RealmKey, ((MBObjectBase)realm).StringId);
				Store.AddDeed(Standing.Date() + "  " + him.Name + " raised a banner at " + seat.Name + ".");
				Log.Write("the bastard rises: " + him.Name + " of " + house.Name +
					" holds " + seat.Name + ", " + gone + " house(s) went over, war=" + Cfg.BastardWar);

				Tell(him, house, realm, seat, gone, heir);
			}
			catch (Exception e)
			{
				Log.Write("the rising failed: " + e);
			}
		}

		// ------------------------------------------------------------------
		// making a person

		// Born of the dead ruler, and therefore of your line. CreateSpecialHero
		// wants a template to build from, so we take one from your own culture
		// and then overwrite what matters: the parents, the name, the age and
		// the story on their encyclopedia page.
		private static Hero Sire(Hero dead, Settlement seat)
		{
			try
			{
				CultureObject culture = (dead.Culture != null) ? dead.Culture : ((Clan.PlayerClan != null) ? Clan.PlayerClan.Culture : null);
				CharacterObject template = Template(culture);
				if (template == null)
				{
					Log.Write("no character template to build a bastard from");
					return null;
				}
				int age = (int)Math.Max(Cfg.BastardMinAge, Math.Min(Cfg.BastardMaxAge, dead.Age - Cfg.BastardBornWhen));
				Hero him = HeroCreator.CreateSpecialHero(template, seat, null, null, age);
				if (him == null)
				{
					return null;
				}

				// Wake him up.
				//
				// CreateSpecialHero hands back a hero in NotSpawned state -
				// every vanilla caller follows it with ChangeState(Active),
				// and without that he is not IsActive, so the game will never
				// give him a party, never let him command, and never place him
				// anywhere. He would have ruled a kingdom from nowhere, his
				// castle would have had no defender, and the war declared in
				// his name would never have been fought by anyone.
				try
				{
					him.ChangeState(Hero.CharacterStates.Active);
				}
				catch (Exception ae)
				{
					Log.Write("the bastard would not wake: " + ae.Message);
				}
				try
				{
					// And put him somewhere, so he exists on the map at the
					// castle he has just taken.
					EnterSettlementAction.ApplyForCharacterOnly(him, seat);
				}
				catch (Exception ee)
				{
					Log.Once("bastardplace", "the bastard could not be placed: " + ee.Message);
				}

				string given = him.FirstName != null ? him.FirstName.ToString() : "The Bastard";
				string surname = Surname(seat);
				try
				{
					him.SetName(new TextObject("{=!}" + given + " " + surname, (Dictionary<string, object>)null),
						new TextObject("{=!}" + given, (Dictionary<string, object>)null));
				}
				catch (Exception ne)
				{
					Log.Write("the name would not take: " + ne.Message);
				}

				try
				{
					him.EncyclopediaText = new TextObject("{=!}" + Story(him, dead, seat, given, surname), (Dictionary<string, object>)null);
					him.IsKnownToPlayer = true;
				}
				catch
				{
				}
				return him;
			}
			catch (Exception e)
			{
				Log.Write("siring the bastard failed: " + e.Message);
				return null;
			}
		}

		// He stops being his mother's surname and becomes his house's.
		private static void Rename(Hero him)
		{
			try
			{
				if (him == null)
				{
					return;
				}
				string given = (him.FirstName != null) ? him.FirstName.ToString() : "The Bastard";
				string blade = Lore.Blade();
				him.SetName(new TextObject("{=!}" + given + " " + blade, (Dictionary<string, object>)null),
					new TextObject("{=!}" + given, (Dictionary<string, object>)null));
				Log.Write(given + " takes the name " + blade);
			}
			catch (Exception e)
			{
				Log.Once("bastardrename", "he kept his old name: " + e.Message);
			}
		}

		// Your blood. This is the whole point: not a pretender with a story,
		// but a half-sibling of your heir with a claim as real as theirs.
		private static void Claim(Hero him, Hero dead)
		{
			try
			{
				if (him == null || dead == null)
				{
					return;
				}
				if (dead.IsFemale)
				{
					him.Mother = dead;
				}
				else
				{
					him.Father = dead;
				}
			}
			catch (Exception e)
			{
				Log.Write("the parentage would not take: " + e.Message);
			}
		}

		// A lord template of the right culture to build the hero from.
		private static CharacterObject Template(CultureObject culture)
		{
			try
			{
				List<CharacterObject> all = CharacterObject.All.Where((CharacterObject c) =>
					c != null && c.IsHero && c.Occupation == Occupation.Lord && c.Culture == culture).ToList();
				if (all.Count == 0)
				{
					all = CharacterObject.All.Where((CharacterObject c) => c != null && c.IsHero && c.Occupation == Occupation.Lord).ToList();
				}
				if (all.Count == 0)
				{
					return null;
				}
				List<CharacterObject> men = all.Where((CharacterObject c) => !c.IsFemale).ToList();
				return Pick(men.Count > 0 ? men : all);
			}
			catch
			{
				return null;
			}
		}

		// ------------------------------------------------------------------
		// the house

		private static Clan Raise(Hero him, Settlement seat, Clan yours, string name)
		{
			try
			{
				Clan house = Clan.CreateClan("wad_bastard_" + CourtBehavior.Today());
				if (house == null)
				{
					return null;
				}
				Set(house, "Name", new TextObject("{=!}" + Lore.HouseName(), (Dictionary<string, object>)null));
				Set(house, "InformalName", new TextObject("{=!}" + name, (Dictionary<string, object>)null));
				house.Culture = (yours != null) ? yours.Culture : him.Culture;
				// A null banner is not survivable. KingdomManager.CreateKingdom
				// registers the kingdom BEFORE it initialises it, so a throw
				// while reading the founder's banner leaves a nameless,
				// half-built kingdom in the save for good.
				house.Banner = Reversed(yours) ?? Banner.CreateRandomClanBanner(-1);
				if (house.Banner != null)
				{
					try
					{
						// These are not decoration. Once the house is crowned
						// the kingdom takes its colours from Color and Color2
						// and repaints the banner with them - Color on the
						// ground, Color2 on every device. So Color2 has to be
						// the DEVICE colour, not the background's second slot,
						// and the two must differ or the sigil vanishes into
						// the field it is drawn on.
						uint ground = house.Banner.GetPrimaryColor();
						uint charge = house.Banner.GetFirstIconColor();
						if (ground == charge || Bad(charge))
						{
							charge = house.Banner.GetSecondaryColor();
						}
						if (ground == charge || Bad(charge))
						{
							// Nothing legible to work with, so leave the three
							// override fields at zero - that disarms the
							// repaint and lets the banner stand as drawn. But
							// Color and Color2 still have to be SET: a clan
							// starts them at zero, and zero is transparent
							// black everywhere a faction colour is used - the
							// map, troop cloth, sails. Dull is fine; invisible
							// is not.
							house.Color = ground;
							house.Color2 = ground;
							Log.Write("the bastard's arms were left as drawn: no second colour to give them");
						}
						else
						{
							house.Color = ground;
							house.Color2 = charge;
							Set(house, "BannerBackgroundColorPrimary", ground);
							Set(house, "BannerBackgroundColorSecondary", ground);
							Set(house, "BannerIconColor", charge);
							Log.Write("the house flies " + Hex(ground) + " with its device in " + Hex(charge));
						}
					}
					catch (Exception be)
					{
						Log.Write("the bastard's colours would not take: " + be.Message);
					}
				}
				Set(house, "Tier", Cfg.BastardTier);
				house.SetLeader(him);
				// The public call, not a reflected write to the private
				// setter: it also sets InitialHomeSettlement and pushes the
				// new home onto every member of the house.
				try
				{
					house.SetInitialHomeSettlement(seat);
				}
				catch (Exception he)
				{
					Log.Once("bastardhome", "the house has no seat recorded: " + he.Message);
				}
				// #8: the two things every vanilla clan-creation path does and
				// this one did not. Without a mid settlement, changing the
				// player character onto him can throw while his clan is still
				// its own faction.
				Call(house, "CalculateMidSettlement");
				Announce(house);
				return house;
			}
			catch (Exception e)
			{
				Log.Write("raising the house failed: " + e.Message);
				return null;
			}
		}

		// Your arms, the colours swapped.
		//
		// Not a new sigil - the same one, worn wrong, which is what a bastard
		// branch does and why it reads as an insult from across a field.
		//
		// This rendered as a flat colour with no device at all, and the reason
		// is worth writing down. "Secondary" on a banner is NOT the sigil:
		// entry [0] is the BACKGROUND and carries two colour slots, and
		// GetSecondaryColorId reads the second of those. The device lives at
		// entry [1] and upwards. So swapping primary against secondary
		// shuffled the background against itself and never touched the charge.
		//
		// It could not have worked anyway. The moment a clan belongs to a
		// kingdom the game runs UpdateBannerColorsAccordingToKingdom, which
		// forces BOTH background slots to one colour - so by the time we read
		// them they were already identical, the swap was a no-op, and painting
		// the device in "primary" painted it in exactly the colour of the
		// ground behind it. A flat field, deterministically, every time.
		//
		// The real reversal is ground against charge.
		private static Banner Reversed(Clan yours)
		{
			try
			{
				if (yours == null || yours.Banner == null)
				{
					return null;
				}
				Banner src = yours.Banner;
				if (src.GetBannerDataListCount() < 2)
				{
					// No device to reverse. Say so rather than hand back a
					// half-changed banner: the caller has a fallback, and it
					// is better than a blank field.
					Log.Write("your arms carry no device, so the bastard takes arms of his own");
					return null;
				}
				uint ground = src.GetPrimaryColor();
				uint charge = src.GetFirstIconColor();
				if (ground == charge || Bad(ground) || Bad(charge))
				{
					Log.Write("your arms would not reverse legibly, so the bastard takes arms of his own");
					return null;
				}
				Banner b = new Banner(src);
				// Say so out loud. This is the one part of a rising that
				// leaves no other trace in the log, and it is the part that
				// was silently broken for three versions - so record the two
				// colours and let a future log prove it rather than implying
				// it by saying nothing.
				Log.Write("arms reversed: ground " + Hex(charge) + " over device " + Hex(ground) +
					" across " + src.GetBannerDataListCount() + " layer(s)");
				// The ground takes the colour the device wore. Both slots the
				// same, because a kingdom flattens them regardless and it is
				// better to know what we are looking at.
				b.ChangeBackgroundColor(charge, charge);
				// And the device takes the colour the ground wore. This covers
				// every icon layer and both colour slots of each, which the
				// old SetIconColorId did not - it reached one slot of one
				// layer and threw on a banner that had no layer to reach.
				b.ChangeIconColors(ground);
				return b;
			}
			catch (Exception e)
			{
				Log.Write("the banner would not reverse: " + e.Message);
				return null;
			}
		}

		private static string Hex(uint colour)
		{
			return "0x" + colour.ToString("X8");
		}

		// What the palette hands back for an id it does not recognise.
		private static bool Bad(uint colour)
		{
			return colour == 0xDEADBEEFu || colour == 0xFFFFFFFFu;
		}

		// Call a public method on the clan if this build has it, and shrug if
		// it does not. Used for the housekeeping calls that differ between
		// game versions and are not worth a hard dependency.
		private static void Call(Clan c, string method)
		{
			try
			{
				MethodInfo m = AccessTools.Method(typeof(Clan), method, (Type[])null, (Type[])null);
				if (m != null && m.GetParameters().Length == 0)
				{
					m.Invoke(c, null);
				}
			}
			catch (Exception e)
			{
				Log.Once("clancall", method + " failed on the bastard's house: " + e.Message);
			}
		}

		// Tell the rest of the game a clan now exists, so war stances and the
		// faction lists are rebuilt for it.
		private static void Announce(Clan house)
		{
			try
			{
				object d = AccessTools.Property(typeof(CampaignEventDispatcher), "Instance")?.GetValue(null, null);
				MethodInfo m = (d == null) ? null : AccessTools.Method(d.GetType(), "OnClanCreated", (Type[])null, (Type[])null);
				if (m == null)
				{
					return;
				}
				ParameterInfo[] ps = m.GetParameters();
				object[] args = new object[ps.Length];
				for (int i = 0; i < ps.Length; i++)
				{
					args[i] = (ps[i].ParameterType == typeof(Clan))
						? house
						: (ps[i].ParameterType.IsValueType ? Activator.CreateInstance(ps[i].ParameterType) : null);
				}
				m.Invoke(d, args);
			}
			catch (Exception e)
			{
				Log.Once("clanannounce", "the new house was not announced: " + e.Message);
			}
		}

		// He was never born. Used when the house cannot be raised, so the save
		// is not left holding a clanless stranger with our backstory on him.
		private static void Unmake(Hero him)
		{
			try
			{
				if (him != null && him.IsAlive)
				{
					KillCharacterAction.ApplyByRemove(him, false);
				}
			}
			catch (Exception e)
			{
				Log.Once("unmake", "the stillborn bastard could not be removed: " + e.Message);
			}
		}

		private static void Set(Clan c, string prop, object value)
		{
			try
			{
				PropertyInfo p = AccessTools.Property(typeof(Clan), prop);
				MethodInfo setter = (p != null) ? p.GetSetMethod(true) : null;
				if (setter != null)
				{
					setter.Invoke(c, new object[1] { value });
				}
			}
			catch (Exception e)
			{
				Log.Once("clanset", "could not set " + prop + " on the bastard's house: " + e.Message);
			}
		}

		// ------------------------------------------------------------------
		// the crown

		// The game's own founding path - the one used when a player declares
		// their own kingdom. It registers the kingdom, names it, takes the
		// banner and colours from the founding house, seats them as the ruling
		// clan and announces it. A clan cannot be joined by other houses; only
		// a kingdom can, which is what makes the defection below possible.
		private static Kingdom Crown(Clan house, Hero him, Settlement seat)
		{
			try
			{
				if (house == null || house.Leader == null || house.Culture == null)
				{
					return null;
				}
				object manager = typeof(Campaign).GetField("KingdomManager")?.GetValue(Campaign.Current);
				MethodInfo create = (manager == null) ? null : AccessTools.Method(manager.GetType(), "CreateKingdom", (Type[])null, (Type[])null);
				if (create == null)
				{
					Log.Write("no kingdom founder on this build; the bastard stays a house");
					return null;
				}
				// Named for the blade he carried out of your hall, which is
				// the Blackfyre move: the sword was the argument, so the sword
				// is the name.
				string realmName = Lore.RealmName();
				TextObject full = new TextObject("{=!}" + realmName, (Dictionary<string, object>)null);
				TextObject brief = new TextObject("{=!}" + Lore.RealmInformal(), (Dictionary<string, object>)null);
				TextObject story = new TextObject("{=!}" + Lore.RealmStory(house, him, seat), (Dictionary<string, object>)null);

				ParameterInfo[] ps = create.GetParameters();
				object[] args = new object[ps.Length];
				int text = 0;
				for (int i = 0; i < ps.Length; i++)
				{
					Type pt = ps[i].ParameterType;
					if (pt == typeof(Clan))
					{
						args[i] = house;
					}
					else if (pt == typeof(CultureObject))
					{
						args[i] = house.Culture;
					}
					else if (pt == typeof(TextObject))
					{
						// In order: kingdomName, informalName, then the three
						// encyclopedia fields. The story goes in the first of
						// those, which is the body of the kingdom's page.
						text++;
						args[i] = (text == 1) ? full : ((text == 2) ? brief : ((text == 3) ? story : null));
					}
					else
					{
						args[i] = ps[i].HasDefaultValue ? ps[i].DefaultValue : null;
					}
				}
				create.Invoke(manager, args);
				Kingdom born = house.Kingdom;
				if (born != null)
				{
					Log.Write("the bastard is crowned: " + born.Name);
					Store.AddDeed(Standing.Date() + "  " + born.Name + " was proclaimed.");
				}
				return born;
			}
			catch (Exception e)
			{
				Log.Write("crowning the bastard failed: " + e.Message);
				return null;
			}
		}

		// ------------------------------------------------------------------
		// the houses that go

		// A third of your sworn houses, chosen at random rather than by any
		// score. There is no ledger behind this any more and that is the point:
		// nobody in the realm knew who would go until the banner went up.
		private static int Defect(Kingdom yours, Kingdom realm, Clan house, float share)
		{
			int gone = 0;
			try
			{
				// Sworn houses only. Excluding the ruling clan matters: moving
				// it fires ChangeRulingClanAction, which would hand the crown
				// itself to the breakaway realm in a single call.
				List<Clan> sworn = yours.Clans.Where((Clan c) =>
					c != null && c != Clan.PlayerClan && c != house && c != yours.RulingClan
					&& !c.IsEliminated && c.Leader != null && c.Leader.IsAlive).ToList();
				if (sworn.Count == 0)
				{
					return 0;
				}
				Shuffle(sworn);
				int want = Math.Max(1, (int)Math.Round(sworn.Count * share));
				foreach (Clan c in sworn)
				{
					if (gone >= want)
					{
						break;
					}
					// You are holding their child. They will think again.
					Held held = Wardship.From(c);
					if (held != null && held.Hostage && !held.Forfeit)
					{
						Log.Write("  " + c.Name + " stays - you hold their blood");
						continue;
					}
					try
					{
						ChangeKingdomAction.ApplyByJoinToKingdomByDefection(c, yours, realm, CampaignTime.Zero, true);
						gone++;
						Log.Write("  " + c.Name + " went over");
					}
					catch (Exception de)
					{
						Log.Once("defect", "a house could not go over: " + de.Message);
					}
				}
			}
			catch (Exception e)
			{
				Log.Write("the defections failed: " + e.Message);
			}
			return gone;
		}

		// ------------------------------------------------------------------
		// and which of them are you

		private static void Tell(Hero him, Clan house, Kingdom realm, Settlement seat, int gone, Hero heir)
		{
			try
			{
				string body =
					him.Name + " has taken " + seat.Name + " and will not give it back.\n\n" +
					realm.Name + " is proclaimed, and " +
					((gone > 0)
						? (gone + " house" + ((gone == 1) ? " has" : "s have") + " gone over to him.")
						: "no house has gone to him yet, which is not the same as none ever will.") +
					(Cfg.BastardWar ? "\n\nThere is a war on as of this morning." : "") +
					"\n\nHe flies your arms with the colours reversed, and he has as much of " +
					((heir != null) ? heir.Name.ToString() : "your heir") + "'s blood in him as they do.\n\n" +
					"Whose side of this are you?";
				string yours = (heir != null) ? ("Stay as " + heir.Name) : "Stay with your house";
				string his = "Take up the banner as " + him.Name;
				Inquiry.Confirm("Two Houses", body, yours, his,
					delegate
					{
						Log.Write("you stayed with your own house");
					},
					delegate
					{
						Become(him);
					});
			}
			catch (Exception e)
			{
				Log.Write("the choice could not be offered: " + e.Message);
			}
		}

		// Play on as the bastard.
		private static void Become(Hero him)
		{
			try
			{
				if (him == null || !him.IsAlive)
				{
					return;
				}
				MethodInfo m = AccessTools.Method("TaleWorlds.CampaignSystem.Actions.ChangePlayerCharacterAction:Apply", (Type[])null, (Type[])null);
				if (m == null)
				{
					Log.Write("this build will not let the player change character");
					Flow.Notify("You cannot take up the banner on this build.");
					return;
				}
				m.Invoke(null, new object[1] { him });
				Log.Write("you took up the bastard's banner as " + him.Name);
				Store.AddDeed(Standing.Date() + "  You took up the banner yourself.");
			}
			catch (Exception e)
			{
				Log.Write("taking up the banner failed: " + e.Message);
			}
		}

		// ------------------------------------------------------------------
		// bits and pieces

		// What your house holds and has not already promised to somebody.
		internal static List<Settlement> Fiefs()
		{
			List<Settlement> list = new List<Settlement>();
			try
			{
				Clan mine = Clan.PlayerClan;
				if (mine == null)
				{
					return list;
				}
				foreach (Settlement s in mine.Settlements)
				{
					// Every town and castle the house holds, the ancestral
					// seat included.
					//
					// There was a filter here that skipped the home settlement,
					// on the theory that losing it would leave the house
					// pointing at a castle the enemy holds. That is not true -
					// the game re-runs its own "most suitable home" search
					// whenever a clan gains or loses a fortification - and the
					// filter had a real cost: it removed exactly one
					// fortification from the count the offer is gated on, so a
					// house with a town and a castle failed a minimum of two
					// and was never offered the bastard at all.
					if (s != null && (s.IsTown || s.IsCastle))
					{
						list.Add(s);
					}
				}
			}
			catch
			{
			}
			return list;
		}

		// The regional surname a bastard is given, by where they were born.
		private static string Surname(Settlement seat)
		{
			return Surnames.Of(seat);
		}

		private static string Story(Hero him, Hero dead, Settlement seat, string given, string surname)
		{
			return Lore.Life(him, dead, seat, given, surname);
		}

		private static T Pick<T>(List<T> list)
		{
			if (list == null || list.Count == 0)
			{
				return default(T);
			}
			return list[MBRandom.RandomInt(list.Count)];
		}

		private static void Shuffle<T>(List<T> list)
		{
			for (int i = list.Count - 1; i > 0; i--)
			{
				int j = MBRandom.RandomInt(i + 1);
				T tmp = list[i];
				list[i] = list[j];
				list[j] = tmp;
			}
		}

		// What the court screen says about it.
		internal static string Summary()
		{
			try
			{
				if (!Cfg.Bastard)
				{
					return null;
				}
				string state = Store.Get(RisenKey);
				if (state == "declined")
				{
					return "  There was no man at the gate. There never was.";
				}
				if (state == "failed")
				{
					// Something went wrong raising him. Say nothing rather
					// than announce a war that never started.
					return null;
				}
				if (!Risen)
				{
					return null;
				}
				Clan house = House;
				if (house == null)
				{
					return "  The banner was raised against you once, and it was put down.";
				}
				Hero him = Head;
				return "  " + house.Name + ((him != null) ? (" under " + him.Name) : "") +
					((house.Kingdom != null) ? (", holding " + house.Kingdom.Name) : "") + ".";
			}
			catch
			{
				return null;
			}
		}
	}
}
