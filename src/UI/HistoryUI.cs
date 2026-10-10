using System;
using System.Collections.Generic;
using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;

namespace WardensAndDragons.UI
{
	// One paragraph of a history in the encyclopedia.
	public sealed class HistoryLineVM : ViewModel
	{
		private string _text;

		public HistoryLineVM(string text)
		{
			_text = text;
		}

		[DataSourceProperty]
		public string Text
		{
			get { return _text; }
			set { if (value != _text) { _text = value; OnPropertyChangedWithValue(value, "Text"); } }
		}
	}

	// Shared by the three pages: a summary at the head, and a "History"
	// section that folds open and shut like the game's own.
	internal static class HistoryView
	{
		internal static void Fill(MBBindingList<HistoryLineVM> lines, IEnumerable<string> paras)
		{
			lines.Clear();
			foreach (string p in paras)
			{
				if (!string.IsNullOrEmpty(p))
				{
					lines.Add(new HistoryLineVM(p));
				}
			}
		}

		internal static string Xml(bool withSummary)
		{
			return @"
<ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' StackLayout.LayoutMethod='VerticalTopToBottom'>
  <Children>" + (withSummary ? @"
    <RichTextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' Brush='Encyclopedia.SubPage.Info.Text' Brush.FontSize='22' Text='@WadSummary' IsVisible='@HasWadSummary' MarginTop='15' MarginLeft='20' MarginRight='20'/>" : "") + @"
    <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' StackLayout.LayoutMethod='VerticalTopToBottom' IsVisible='@HasWadHistory'>
      <Children>
        <EncyclopediaDivider Id='WadHistoryDivider' MarginTop='25' Parameter.Title='@WadHistoryTitle' Parameter.ItemList='..\WadHistoryParent' GamepadNavigationIndex='0'/>
        <Widget Id='WadHistoryParent' DoNotAcceptEvents='true' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' MarginTop='10' MarginLeft='20' MarginRight='20'>
          <Children>
            <ListPanel DataSource='{WadHistory}' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' StackLayout.LayoutMethod='VerticalTopToBottom'>
              <ItemTemplate>
                <RichTextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' Brush='Encyclopedia.SubPage.Info.Text' Text='@Text' MarginBottom='12'/>
              </ItemTemplate>
            </ListPanel>
          </Children>
        </Widget>
      </Children>
    </ListPanel>
  </Children>
</ListPanel>";
		}
	}

	internal abstract class HistoryMixinBase<T> : BaseViewModelMixin<T> where T : ViewModel
	{
		private readonly MBBindingList<HistoryLineVM> _lines = new MBBindingList<HistoryLineVM>();
		private bool _has;
		private bool _hasSummary;
		private string _summary = "";
		private string _title = "History";

		protected HistoryMixinBase(T vm) : base(vm)
		{
			Update();
		}

		public override void OnRefresh()
		{
			Update();
		}

		protected abstract void Build(MBBindingList<HistoryLineVM> lines, out string summary, out string title);

		private void Update()
		{
			try
			{
				string summary = null;
				string title = "History";
				_lines.Clear();
				if (Cfg.Histories && Store.Initialized)
				{
					Build(_lines, out summary, out title);
				}
				HasWadHistory = _lines.Count > 0;
				WadSummary = summary ?? "";
				HasWadSummary = !string.IsNullOrEmpty(summary);
				WadHistoryTitle = title;
			}
			catch (Exception e)
			{
				Log.Once("histmixin" + typeof(T).Name, "history: encyclopedia section failed: " + e.Message);
			}
		}

		[DataSourceProperty]
		public MBBindingList<HistoryLineVM> WadHistory => _lines;

		[DataSourceProperty]
		public bool HasWadHistory
		{
			get { return _has; }
			set { if (value != _has) { _has = value; ViewModel?.OnPropertyChangedWithValue(value, "HasWadHistory"); } }
		}

		[DataSourceProperty]
		public bool HasWadSummary
		{
			get { return _hasSummary; }
			set { if (value != _hasSummary) { _hasSummary = value; ViewModel?.OnPropertyChangedWithValue(value, "HasWadSummary"); } }
		}

		[DataSourceProperty]
		public string WadSummary
		{
			get { return _summary; }
			set { if (value != _summary) { _summary = value; ViewModel?.OnPropertyChangedWithValue(value, "WadSummary"); } }
		}

		[DataSourceProperty]
		public string WadHistoryTitle
		{
			get { return _title; }
			set { if (value != _title) { _title = value; ViewModel?.OnPropertyChangedWithValue(value, "WadHistoryTitle"); } }
		}
	}

	[ViewModelMixin("RefreshValues", true)]
	internal sealed class HeroHistoryMixin : HistoryMixinBase<EncyclopediaHeroPageVM>
	{
		public HeroHistoryMixin(EncyclopediaHeroPageVM vm) : base(vm) { }

		protected override void Build(MBBindingList<HistoryLineVM> lines, out string summary, out string title)
		{
			summary = null;
			title = "History";
			Hero h = ViewModel?.Obj as Hero;
			if (h == null)
			{
				return;
			}
			List<string> paras = new List<string>();
			string nem = Nemesis.Describe(h);
			if (!string.IsNullOrEmpty(nem))
			{
				paras.Add(nem);
			}
			paras.AddRange(Chronicler.Prose(History.KeyOf(h), h));
			HistoryView.Fill(lines, paras);
		}
	}

	[ViewModelMixin("Refresh", true)]
	internal sealed class ClanHistoryMixin : HistoryMixinBase<EncyclopediaClanPageVM>
	{
		public ClanHistoryMixin(EncyclopediaClanPageVM vm) : base(vm) { }

		protected override void Build(MBBindingList<HistoryLineVM> lines, out string summary, out string title)
		{
			Clan c = ViewModel?.Obj as Clan;
			summary = Chronicler.HouseSummary(c);
			title = "History of the House";
			if (c != null)
			{
				HistoryView.Fill(lines, Chronicler.Prose(History.KeyOf(c), null));
			}
		}
	}

	[ViewModelMixin("Refresh", true)]
	internal sealed class RealmHistoryMixin : HistoryMixinBase<EncyclopediaFactionPageVM>
	{
		public RealmHistoryMixin(EncyclopediaFactionPageVM vm) : base(vm) { }

		protected override void Build(MBBindingList<HistoryLineVM> lines, out string summary, out string title)
		{
			Kingdom k = ViewModel?.Obj as Kingdom;
			summary = Chronicler.RealmSummary(k);
			title = "History of the Realm";
			if (k != null)
			{
				HistoryView.Fill(lines, Chronicler.Prose(History.KeyOf(k), null));
			}
		}
	}

	[PrefabExtension("EncyclopediaHeroPage", "descendant::EncyclopediaDivider[@Id='AlliesDivider']")]
	internal sealed class HeroHistoryPrefab : PrefabExtensionInsertPatch
	{
		private readonly XmlDocument _doc = new XmlDocument();
		public override InsertType Type => InsertType.Prepend;
		public HeroHistoryPrefab() { _doc.LoadXml(HistoryView.Xml(false)); }

		[PrefabExtensionXmlDocument(false)]
		public XmlDocument GetPrefabExtension() { return _doc; }
	}

	// House: the summary right under the house's description, then its history.
	[PrefabExtension("EncyclopediaClanPage", "descendant::EncyclopediaDivider[@Id='LeaderDivider']")]
	internal sealed class ClanHistoryPrefab : PrefabExtensionInsertPatch
	{
		private readonly XmlDocument _doc = new XmlDocument();
		public override InsertType Type => InsertType.Prepend;
		public ClanHistoryPrefab() { _doc.LoadXml(HistoryView.Xml(true)); }

		[PrefabExtensionXmlDocument(false)]
		public XmlDocument GetPrefabExtension() { return _doc; }
	}

	[PrefabExtension("EncyclopediaFactionPage", "descendant::EncyclopediaDivider[@Id='LeaderDivider']")]
	internal sealed class RealmHistoryPrefab : PrefabExtensionInsertPatch
	{
		private readonly XmlDocument _doc = new XmlDocument();
		public override InsertType Type => InsertType.Prepend;
		public RealmHistoryPrefab() { _doc.LoadXml(HistoryView.Xml(true)); }

		[PrefabExtensionXmlDocument(false)]
		public XmlDocument GetPrefabExtension() { return _doc; }
	}
}
