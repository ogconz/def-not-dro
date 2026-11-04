namespace SyncMultiplayer
{
	public static class SubroomModeExtension
	{
		public static bool AllowedSelfChallenge(this SubroomMode mode)
		{
			if (GameConsole.Bool["cl_allowselfchallenge"])
			{
				return true;
			}
			if (mode <= SubroomMode.ClippingDuel || mode == SubroomMode.XDS)
			{
				return true;
			}
			return false;
		}

		public static bool DisableMoreTwoChallenge(this SubroomMode mode)
		{
			if (mode == SubroomMode.ClippingDuel || mode - 4 <= SubroomMode.TimeAttack)
			{
				return true;
			}
			return false;
		}

		public static bool IsTime(this SubroomMode subroomMode)
		{
			return subroomMode == SubroomMode.TimeAttack;
		}

		public static bool IsXDS(this SubroomMode subroomMode)
		{
			if (subroomMode != SubroomMode.XDS)
			{
				return subroomMode == SubroomMode.TandemXDS;
			}
			return true;
		}

		public static bool IsTandem(this SubroomMode subroomMode)
		{
			if (subroomMode != SubroomMode.Tandem)
			{
				return subroomMode == SubroomMode.TandemXDS;
			}
			return true;
		}
	}
}
