using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DI;
using PrefsModels;
using anzu;

namespace Services.Anzu.Obstruction
{
	public class AnzuObstructionManagerWrapper
	{
		private static AnzuObstructionManagerWrapper s_instance;

		private List<AnzuObstructionItem> m_observableObstructors = new List<AnzuObstructionItem>();

		private GraphicSettings m_graphics;

		public static AnzuObstructionManagerWrapper instance => s_instance ?? new AnzuObstructionManagerWrapper();

		private AnzuObstructionManagerWrapper()
		{
			s_instance = this;
			InitResolutionChangedCallback();
		}

		private void InitResolutionChangedCallback()
		{
			if (m_graphics == null)
			{
				m_graphics = DependencyInjector.Resolve<GamePrefs>().graphics;
			}
			GraphicSettings graphics = m_graphics;
			graphics.onResolutionChanged = (Action<GraphicSettings.ResolutionInfo>)Delegate.Combine(graphics.onResolutionChanged, new Action<GraphicSettings.ResolutionInfo>(OnResolutionChanged));
		}

		private void OnResolutionChanged(GraphicSettings.ResolutionInfo _)
		{
			UniTask.Post(UpdateObstructors, PlayerLoopTiming.LastPostLateUpdate);
			void UpdateObstructors()
			{
				foreach (AnzuObstructionItem observableObstructor in m_observableObstructors)
				{
					observableObstructor.UpdateObstruction();
				}
			}
		}

		public void AddElement(AnzuObstructionItem obstructionItem)
		{
			m_observableObstructors.Add(obstructionItem);
			ObstructionManager.AddElement(obstructionItem.obstructor);
		}

		public void RemoveElement(AnzuObstructionItem obstructionItem)
		{
			ObstructionManager.RemoveElement(obstructionItem.obstructor);
			m_observableObstructors.Remove(obstructionItem);
		}
	}
}
