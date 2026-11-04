using System;
using System.Collections.Generic;
using CarX.Gameplay.ClippingZone;
using SimpleJSON;

namespace SyncMultiplayer
{
	public class SubroomInfoRaceResult
	{
		[Serializable]
		public class XDSResultDetails
		{
			[Serializable]
			public class Sector
			{
				public int b;

				public string l;

				public string c;

				public List<int> z = new List<int>();

				public Sector Clone()
				{
					return new Sector
					{
						b = b,
						l = l,
						c = c,
						z = new List<int>(z)
					};
				}
			}

			public static readonly string OpponentHasDQStr = "+";

			public DisqualifyReason drl;

			public DisqualifyReason drc;

			public string tl;

			public string tc;

			public List<Sector> s = new List<Sector>();

			public XDSResultDetails Clone()
			{
				List<Sector> list = new List<Sector>(s.Count);
				for (int i = 0; i < s.Count; i++)
				{
					list.Add(s[i].Clone());
				}
				return new XDSResultDetails
				{
					drl = drl,
					drc = drc,
					tl = tl,
					tc = tc,
					s = list
				};
			}
		}

		public bool isFinished;

		public DisqualifyReason disqualifyReason;

		[JsonSerialize]
		private float m_tag;

		public string asString = string.Empty;

		public Dictionary<string, string> detailedResult;

		[JsonIgnore]
		private XDSResultDetails m_cachedXDSDetails;

		public float sortOrder => m_tag;

		public float time
		{
			get
			{
				if (!(m_tag <= 0f))
				{
					return 0f;
				}
				return 0f - m_tag;
			}
		}

		public float points
		{
			get
			{
				if (!(m_tag >= 0f))
				{
					return 0f;
				}
				return m_tag;
			}
		}

		public SubroomInfoRaceResult()
		{
		}

		public SubroomInfoRaceResult(int points, DisqualifyReason reason = DisqualifyReason.Unknown)
		{
			m_tag = points;
			asString = UIUtils.DigitsWithSeparate(points);
			disqualifyReason = reason;
			PostConstruction();
		}

		public SubroomInfoRaceResult(int firstRacePoints, int secondRacePoints, DisqualifyReason reason = DisqualifyReason.Unknown)
		{
			m_tag = firstRacePoints + secondRacePoints;
			asString = UIUtils.DigitsWithSeparate(firstRacePoints) + " - " + UIUtils.DigitsWithSeparate(secondRacePoints) + " (" + UIUtils.DigitsWithSeparate((int)points) + ")";
			disqualifyReason = reason;
			PostConstruction();
		}

		public SubroomInfoRaceResult(float time, DisqualifyReason reason = DisqualifyReason.Unknown)
		{
			m_tag = 0f - time;
			asString = UIUtils.TimeToMSMString(time);
			disqualifyReason = reason;
			PostConstruction(reversed: true);
		}

		public SubroomInfoRaceResult(DriftControllerXDS xds, DisqualifyReason reason, bool isLeader)
		{
			disqualifyReason = reason;
			m_tag = (reason.IsEmpty() ? xds.score : 0f);
			asString = DriftControllerXDS.ConvertScoresToResultString(m_tag);
			int num = ((xds.sectors != null) ? (xds.sectors.Length - 1) : 0);
			XDSResultDetails xDSResultDetails = new XDSResultDetails();
			if (isLeader)
			{
				xDSResultDetails.drl = reason;
				xDSResultDetails.tl = asString;
			}
			else
			{
				xDSResultDetails.drc = reason;
				xDSResultDetails.tc = asString;
			}
			for (int i = 0; i < num; i++)
			{
				XDSResultDetails.Sector sector = new XDSResultDetails.Sector();
				xDSResultDetails.s.Add(sector);
				DriftControllerXDS.SectorInfo sectorInfo = xds.sectors[i];
				float finalScores = sectorInfo.GetFinalScores();
				if (isLeader)
				{
					sector.l = DriftControllerXDS.ConvertScoresToResultString(finalScores);
				}
				else
				{
					sector.c = DriftControllerXDS.ConvertScoresToResultString(finalScores);
				}
				sector.b = (sectorInfo.info.isblank ? 1 : 0);
				foreach (BaseClipping clippingObject in xds.clippingObjects)
				{
					if (clippingObject != null && clippingObject.sector == i)
					{
						sector.z.Add((clippingObject.state == BaseClipping.State.Activated) ? 1 : 0);
					}
				}
			}
			SetXDSResultDatails(xDSResultDetails);
			PostConstruction();
		}

		public SubroomInfoRaceResult CombineWithSecondTandemRace(DriftControllerXDS xds, DisqualifyReason reason, bool isLeader)
		{
			float num = (reason.IsEmpty() ? xds.score : 0f);
			SubroomInfoRaceResult subroomInfoRaceResult = new SubroomInfoRaceResult();
			subroomInfoRaceResult.m_tag = points + num;
			subroomInfoRaceResult.asString = DriftControllerXDS.ConvertScoresToResultString(subroomInfoRaceResult.points);
			subroomInfoRaceResult.disqualifyReason = reason;
			XDSResultDetails xDSResultDetails = GetXDSResultDetails();
			if (isLeader)
			{
				xDSResultDetails.drl = reason;
				xDSResultDetails.tl = DriftControllerXDS.ConvertScoresToResultString(num);
			}
			else
			{
				xDSResultDetails.drc = reason;
				xDSResultDetails.tc = DriftControllerXDS.ConvertScoresToResultString(num);
			}
			int num2 = ((xds.sectors != null) ? (xds.sectors.Length - 1) : 0);
			for (int i = 0; i < num2; i++)
			{
				XDSResultDetails.Sector sector = null;
				if (i < xDSResultDetails.s.Count)
				{
					sector = xDSResultDetails.s[i];
				}
				else
				{
					sector = new XDSResultDetails.Sector();
					xDSResultDetails.s.Add(sector);
				}
				DriftControllerXDS.SectorInfo sectorInfo = xds.sectors[i];
				float finalScores = sectorInfo.GetFinalScores();
				if (isLeader)
				{
					sector.l = DriftControllerXDS.ConvertScoresToResultString(finalScores);
				}
				else
				{
					sector.c = DriftControllerXDS.ConvertScoresToResultString(finalScores);
				}
				sector.b = (sectorInfo.info.isblank ? 1 : 0);
				foreach (BaseClipping clippingObject in xds.clippingObjects)
				{
					if (clippingObject != null && clippingObject.sector == i)
					{
						sector.z.Add((clippingObject.state == BaseClipping.State.Activated) ? 1 : 0);
					}
				}
			}
			subroomInfoRaceResult.SetXDSResultDatails(xDSResultDetails);
			return subroomInfoRaceResult;
		}

		public void PostConstruction(bool reversed = false)
		{
			isFinished = disqualifyReason == DisqualifyReason.Unknown;
			if (!isFinished)
			{
				m_tag = (reversed ? float.MaxValue : 0f);
			}
		}

		public void SetXDSResultDatails(XDSResultDetails rd)
		{
			if (detailedResult == null)
			{
				detailedResult = new Dictionary<string, string>();
			}
			detailedResult["rd"] = JSON.Serialize(rd);
			m_cachedXDSDetails = rd.Clone();
		}

		public XDSResultDetails GetXDSResultDetails()
		{
			if (m_cachedXDSDetails == null)
			{
				if (detailedResult != null && detailedResult.TryGetValue("rd", out var value))
				{
					m_cachedXDSDetails = JSON.Parse<XDSResultDetails>(value);
				}
				else
				{
					m_cachedXDSDetails = new XDSResultDetails();
				}
			}
			return m_cachedXDSDetails.Clone();
		}

		public SubroomInfoRaceResult Clone()
		{
			SubroomInfoRaceResult subroomInfoRaceResult = new SubroomInfoRaceResult
			{
				isFinished = isFinished,
				asString = asString,
				m_tag = m_tag,
				disqualifyReason = disqualifyReason
			};
			if (detailedResult != null)
			{
				subroomInfoRaceResult.detailedResult = new Dictionary<string, string>(detailedResult);
			}
			return subroomInfoRaceResult;
		}
	}
}
