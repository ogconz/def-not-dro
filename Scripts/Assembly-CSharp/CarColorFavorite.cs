using System;
using SimpleJSON;
using UnityEngine;

public class CarColorFavorite
{
	[JsonIgnore]
	public Guid guid;

	public int favoriteColorsIndex;

	public Color colorMain;

	public Color colorAdditional;

	public Color colorSpecular;

	public Color colorDuo;

	public string strGuid
	{
		get
		{
			return guid.ToString();
		}
		set
		{
			guid = new Guid(value);
		}
	}

	public CarColorFavorite()
	{
		guid = Guid.NewGuid();
		colorMain = Color.white;
		colorAdditional = Color.clear;
		colorSpecular = Color.white;
		colorDuo = Color.white;
	}

	public CarColorFavorite(Color main, Color additional, Color specular, Color duo)
	{
		guid = Guid.NewGuid();
		colorMain = main;
		colorAdditional = additional;
		colorSpecular = specular;
		colorDuo = duo;
	}
}
