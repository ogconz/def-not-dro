namespace Chat.Overlay.UI.State
{
	public class StateContext
	{
		public StateFlag inputFieldFocused { get; private set; }

		public StateFlag inputFieldHovered { get; private set; }

		public StateFlag actionPanelActive { get; private set; }

		public StateFlag actionPanelFocused { get; private set; }

		public StateFlag actionPanelHovered { get; private set; }

		public StateFlag ignorePanelActive { get; private set; }

		public StateFlag windowActive { get; private set; }

		public StateFlag windowHovered { get; private set; }

		public StateFlag windowFocused { get; private set; }

		public StateFlag windowForceShow { get; private set; }

		public StateFlag xrModeActive { get; private set; }

		public StateFlag messageScrollVisible { get; private set; }

		public StateFlag messageAutoScrollMode { get; private set; }

		public StateFlag messageSelecting { get; private set; }

		public StateFlag deviceWithNavigation { get; private set; }

		public StateFlag forceActivateControlLayer { get; private set; }

		public StateFlag navigationLayerActive { get; private set; }

		public StateFlag controlLayerActive { get; private set; }

		public StateFlag showWindow { get; private set; }

		public StateFlag showInputField { get; private set; }

		public void Setup()
		{
			inputFieldFocused = new StateFlag();
			inputFieldHovered = new StateFlag();
			actionPanelActive = new StateFlag();
			actionPanelFocused = new StateFlag();
			actionPanelHovered = new StateFlag();
			ignorePanelActive = new StateFlag();
			windowActive = new StateFlag();
			windowHovered = new StateFlag();
			windowFocused = new StateFlag();
			windowForceShow = new StateFlag();
			xrModeActive = new StateFlag();
			messageScrollVisible = new StateFlag();
			messageAutoScrollMode = new StateFlag();
			messageSelecting = new StateFlag();
			deviceWithNavigation = new StateFlag();
			forceActivateControlLayer = new StateFlag();
			navigationLayerActive = new StateFlag();
			controlLayerActive = new DependencyStateFlag(DependencyType.Or, actionPanelActive, ignorePanelActive, inputFieldFocused, messageSelecting, windowFocused, forceActivateControlLayer);
			showWindow = new DependencyStateFlag(DependencyType.Or, actionPanelActive, ignorePanelActive, inputFieldFocused, windowHovered, windowForceShow, controlLayerActive);
			showInputField = new DependencyStateFlag(DependencyType.Or, inputFieldFocused, inputFieldHovered, showWindow);
		}

		public void Release()
		{
			showInputField.Dispose();
			showInputField = null;
			showWindow.Dispose();
			showWindow = null;
			controlLayerActive.Dispose();
			controlLayerActive = null;
			navigationLayerActive.Dispose();
			navigationLayerActive = null;
			forceActivateControlLayer.Dispose();
			forceActivateControlLayer = null;
			deviceWithNavigation.Dispose();
			deviceWithNavigation = null;
			messageSelecting.Dispose();
			messageSelecting = null;
			messageAutoScrollMode.Dispose();
			messageAutoScrollMode = null;
			messageScrollVisible.Dispose();
			messageScrollVisible = null;
			xrModeActive.Dispose();
			xrModeActive = null;
			windowForceShow.Dispose();
			windowForceShow = null;
			windowFocused.Dispose();
			windowFocused = null;
			windowHovered.Dispose();
			windowHovered = null;
			windowActive.Dispose();
			windowActive = null;
			ignorePanelActive.Dispose();
			ignorePanelActive = null;
			actionPanelHovered.Dispose();
			actionPanelHovered = null;
			actionPanelFocused.Dispose();
			actionPanelFocused = null;
			actionPanelActive.Dispose();
			actionPanelActive = null;
			inputFieldHovered.Dispose();
			inputFieldHovered = null;
			inputFieldFocused.Dispose();
			inputFieldFocused = null;
		}
	}
}
