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
	// One line of a history in the encyclopedia.
	public sealed class HistoryLineVM : ViewModel
	{
		private string _date;
		private string _text;

		public HistoryLineVM(string date, string text)
		{
			_date = date;
			_text = text;
		}

		[DataSourceProperty]
		public string Date
		{
			get { return _date; }
			set { if (value != _date) { _date = value; OnPropertyChangedWithValue(value, "Date"); } }
		}

		[DataSourceProperty]
		public string Text
		{
			get { return _text; }
			set { if (value != _text) { _text = value; OnPropertyChangedWithValue(value, "Text"); } }
		}
	}

	// The lord's page: a "History" header that folds open and shut like the
	// game's own sections, placed just before the Allies section.
	[ViewModelMixin("RefreshValues", true)]
	internal sealed class HeroHistoryMixin : BaseViewModelMixin<EncyclopediaHeroPageVM>
	{
		private MBBindingList<HistoryLineVM> _lines = new MBBindingList<HistoryLineVM>();
		private bool _has;
		private string _title = "History";

		public HeroHistoryMixin(EncyclopediaHeroPageVM vm) : base(vm)
		{
			Fill();
		}

		public override void OnRefresh()
		{
			Fill();
		}

		private void Fill()
		{
			try
			{
				Hero h = (ViewModel != null) ? (ViewModel.Obj as Hero) : null;
				_lines.Clear();
				if (h != null && Cfg.Histories && Store.Initialized)
				{
					string nem = Nemesis.Describe(h);
					if (!string.IsNullOrEmpty(nem))
					{
						_lines.Add(new HistoryLineVM("", nem));
					}
					foreach (KeyValuePair<string, string> kv in History.Lines(History.KeyOf(h)))
					{
						_lines.Add(new HistoryLineVM(kv.Key, kv.Value));
					}
				}
				HasWadHistory = _lines.Count > 0;
				WadHistoryTitle = "History";
			}
			catch (Exception e)
			{
				Log.Once("histmixin", "history: encyclopedia section failed: " + e.Message);
			}
		}

		[DataSourceProperty]
		public MBBindingList<HistoryLineVM> WadHistory
		{
			get { return _lines; }
		}

		[DataSourceProperty]
		public bool HasWadHistory
		{
			get { return _has; }
			set { if (value != _has) { _has = value; ViewModel?.OnPropertyChangedWithValue(value, "HasWadHistory"); } }
		}

		[DataSourceProperty]
		public string WadHistoryTitle
		{
			get { return _title; }
			set { if (value != _title) { _title = value; ViewModel?.OnPropertyChangedWithValue(value, "WadHistoryTitle"); } }
		}
	}

	[PrefabExtension("EncyclopediaHeroPage", "descendant::EncyclopediaDivider[@Id='AlliesDivider']")]
	internal sealed class HeroHistoryPrefab : PrefabExtensionInsertPatch
	{
		private readonly XmlDocument _doc = new XmlDocument();

		public override InsertType Type => InsertType.Prepend;

		public HeroHistoryPrefab()
		{
			_doc.LoadXml(@"
<ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' StackLayout.LayoutMethod='VerticalTopToBottom' IsVisible='@HasWadHistory' MarginBottom='20'>
  <Children>
    <EncyclopediaDivider Id='WadHistoryDivider' MarginTop='30' Parameter.Title='@WadHistoryTitle' Parameter.ItemList='..\WadHistoryParent' GamepadNavigationIndex='0'/>
    <Widget Id='WadHistoryParent' DoNotAcceptEvents='true' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' MarginTop='10' MarginLeft='30' MarginRight='30'>
      <Children>
        <ListPanel DataSource='{WadHistory}' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' StackLayout.LayoutMethod='VerticalTopToBottom'>
          <ItemTemplate>
            <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' MarginTop='4' StackLayout.LayoutMethod='HorizontalLeftToRight'>
              <Children>
                <RichTextWidget WidthSizePolicy='Fixed' SuggestedWidth='170' HeightSizePolicy='CoverChildren' Brush='Encyclopedia.Stat.DefinitionText' Text='@Date' />
                <RichTextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' Brush='Encyclopedia.Stat.ValueText' Brush.TextHorizontalAlignment='Left' Text='@Text' />
              </Children>
            </ListPanel>
          </ItemTemplate>
        </ListPanel>
      </Children>
    </Widget>
  </Children>
</ListPanel>");
		}

		[PrefabExtensionXmlDocument(false)]
		public XmlDocument GetPrefabExtension()
		{
			return _doc;
		}
	}
}
