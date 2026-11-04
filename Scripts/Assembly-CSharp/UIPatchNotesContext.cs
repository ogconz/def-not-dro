using System.Collections.Generic;
using Binding;
using Binding.Base;
using DI;
using GameInput;
using PrefsModels;
using Services.Patchnote;
using Services.Patchnote.Processors;
using Services.Patchnote.Processors.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPatchNotesContext : BaseBindingBehaviourTarget
{
	private const bool ForceHidePatchNoteUI = false;

	public readonly Property<string> m_titleText = new StringProperty();

	public static bool enteredInGame = false;

	private static readonly string ImagesRoute = "PatchNotes/";

	[SerializeField]
	private Transform m_contentTransform;

	[SerializeField]
	private TextMeshProUGUI m_textPrefab;

	[SerializeField]
	private Transform m_spritePrefab;

	[SerializeField]
	private Scrollbar m_scroll;

	private List<GameObject> m_objectPool = new List<GameObject>();

	private List<Sprite> m_sprites = new List<Sprite>();

	private List<IPatchnoteListProcessor<PatchNoteItem>> m_processors;

	public static bool CheckVersionAndShowPatchNote()
	{
		int version = GameVersion.version;
		if (!CheckPatchNoteLocalization())
		{
			return false;
		}
		string versionAsString = GameVersion.versionAsString;
		GlobalModel globals = DependencyInjector.Resolve<GamePrefs>().globals;
		string lastGameVersion = globals.lastGameVersion;
		if (lastGameVersion == string.Empty)
		{
			globals.lastGameVersion = versionAsString;
			return true;
		}
		if (CompareVersions(version, GameVersion.ParseVersion(lastGameVersion)))
		{
			globals.lastGameVersion = versionAsString;
			return true;
		}
		return false;
	}

	private static bool CompareVersions(int currentVersion, int lastVersion)
	{
		if (currentVersion > lastVersion)
		{
			return true;
		}
		return false;
	}

	public static bool CheckPatchNoteLocalization()
	{
		if (LocalizationService.GetAllKeysWithPrefix("patchnote").Count > 0)
		{
			return true;
		}
		return false;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		m_scroll.value = 1f;
		BringForwardByDepth();
		if (m_processors == null)
		{
			InitProcessors();
		}
		if (m_objectPool.Count == 0)
		{
			FillingPatchNote();
		}
		if (base.owner.IsActive<UIMainMenuContext>())
		{
			base.owner.Get<UIMainMenuContext>().BringForwardByDepthTopPanel();
		}
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
	}

	private void InitProcessors()
	{
		m_processors = new List<IPatchnoteListProcessor<PatchNoteItem>>
		{
			new PatchNotesKeyFillProcessor(),
			new PatchNotesTagsProcessor(),
			new PatchNotesSortProcessor(),
			new PatchNotesTypeProcessor()
		};
	}

	private void FillingPatchNote()
	{
		List<PatchNoteItem> list = new List<PatchNoteItem>();
		foreach (IPatchnoteListProcessor<PatchNoteItem> processor in m_processors)
		{
			processor.Process(list);
		}
		foreach (PatchNoteItem item in list)
		{
			CreateNoteElement(item);
		}
	}

	private void CreateNoteElement(PatchNoteItem note)
	{
		FormatType formatType = note.formatType;
		switch (formatType)
		{
		case FormatType.Title:
			m_titleText.value = note.text;
			break;
		case FormatType.Space:
		case FormatType.Image:
			CreatePrefabOfType(formatType, note.key);
			break;
		default:
			CreatePrefabOfType(formatType, note.text);
			break;
		}
	}

	private void CreatePrefabOfType(FormatType type, string text)
	{
		switch (type)
		{
		case FormatType.Header:
		case FormatType.Text:
			CreateTextPrefab(type, text);
			break;
		case FormatType.Image:
		{
			int num = text.LastIndexOf("_");
			string spriteName = text.Substring(num + 1);
			CreateImagePrefab(spriteName);
			break;
		}
		case FormatType.Space:
			CreateTextPrefab(type, string.Empty);
			break;
		}
	}

	private void CreateTextPrefab(FormatType type, string text)
	{
		TextMeshProUGUI textMeshProUGUI = Object.Instantiate(m_textPrefab, m_contentTransform);
		switch (type)
		{
		case FormatType.Header:
			text = "<size=22><b>" + text + "</b></size>";
			break;
		case FormatType.Space:
			text = "\n";
			break;
		case FormatType.Text:
			text = "<indent=2%>-</indent><indent=5%>" + text + "</indent>";
			break;
		}
		textMeshProUGUI.text = text;
		m_objectPool.Add(textMeshProUGUI.gameObject);
	}

	private void CreateImagePrefab(string spriteName)
	{
		Transform spritePrefab = Object.Instantiate(m_spritePrefab, m_contentTransform);
		ResourcesWrapper.LoadAsync(ImagesRoute + spriteName, delegate(Sprite sprite)
		{
			spritePrefab.GetComponentInChildren<Image>().sprite = sprite;
			if (sprite != null)
			{
				m_sprites.Add(sprite);
				m_objectPool.Add(spritePrefab.gameObject);
			}
			else
			{
				Object.Destroy(spritePrefab.gameObject);
			}
		});
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	public void NavUp()
	{
		m_scroll.value = Mathf.Clamp01(m_scroll.value + m_scroll.size / 2f);
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	public void NavDown()
	{
		m_scroll.value = Mathf.Clamp01(m_scroll.value - m_scroll.size / 2f);
	}

	[GameHotkey(ActionType.NavLeft, false, true)]
	public void NavLeft()
	{
		UIMainMenuContext uIMainMenuContext = base.owner.Get<UIMainMenuContext>();
		if (uIMainMenuContext != null)
		{
			uIMainMenuContext.OnFailedMoveByNavigation(NavigateDirection.Left);
		}
	}
}
