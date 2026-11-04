using System;
using DI;
using UnityEngine;

public abstract class BaseGameState : BaseState
{
	public static Func<bool> escapeHandlers;

	public bool clearUiBySceneNewSceneLoad = true;

	public Transform uguiRoot;

	protected bool m_clearAllContextOnExit = true;

	[Dependency]
	protected GameManager m_gameManager;

	public GameManager gameManager => m_gameManager;

	public ContextOwner contextOwner { get; private set; }

	public static Transform FindUGUIRoot()
	{
		Canvas[] array = UnityEngine.Object.FindObjectsOfType<Canvas>();
		foreach (Canvas canvas in array)
		{
			if (!(canvas.transform.name == "Root"))
			{
				continue;
			}
			if (canvas.transform.childCount > 0)
			{
				for (int j = 0; j < canvas.transform.childCount; j++)
				{
					Transform child = canvas.transform.GetChild(j);
					if (child.name == "Contexts")
					{
						return child;
					}
				}
			}
			return canvas.transform;
		}
		return null;
	}

	private void CreateContextOwner()
	{
		if (uguiRoot == null)
		{
			uguiRoot = FindUGUIRoot();
			if (uguiRoot == null)
			{
				throw new InvalidOperationException("No UIRoot in the scene. Halting UI initialization");
			}
		}
		contextOwner = new ContextOwner(uguiRoot);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		CreateContextOwner();
		if (clearUiBySceneNewSceneLoad)
		{
			States.onAboutToLoadLevel += OnAboutToLoadLevel;
		}
	}

	public override void OnExit()
	{
		if (clearUiBySceneNewSceneLoad)
		{
			States.onAboutToLoadLevel -= OnAboutToLoadLevel;
		}
		if (m_clearAllContextOnExit)
		{
			contextOwner.ClearAll(stateChanged: true);
		}
		base.OnExit();
	}

	public override void OnResume()
	{
		base.OnResume();
		contextOwner.HandleResume();
	}

	public override void OnPaused()
	{
		base.OnPaused();
		contextOwner.HandlePause();
	}

	public override void OnInit()
	{
		base.OnInit();
		this.MakeDependenciesInjected();
	}

	public override void OnUpdate()
	{
		base.OnUpdate();
		if (Input.GetKeyDown(KeyCode.Escape) && (escapeHandlers == null || !escapeHandlers()))
		{
			OnProcessEscape();
		}
	}

	private void ShowExitDialog()
	{
		if (!contextOwner.IsActive<UIBaseDialogContext>() && !(States.mainCurrent is StartState))
		{
			UIMessageBox.ShowQuestion(LocalizationKeys.exit_question.Localized(), delegate
			{
			}, LocalizationKeys.Cancel.Localized(), Application.Quit, LocalizationKeys.Ok.Localized());
		}
	}

	protected virtual void OnProcessEscape()
	{
		if (!contextOwner.HandleEscape())
		{
			if (base.isPopupState)
			{
				States.Pop();
			}
			else if (contextOwner.IsEndContext())
			{
				ShowExitDialog();
			}
		}
	}

	private void OnAboutToLoadLevel()
	{
		if (contextOwner != null)
		{
			contextOwner.ClearAll();
		}
	}
}
