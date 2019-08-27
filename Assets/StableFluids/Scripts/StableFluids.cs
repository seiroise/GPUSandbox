namespace Seiro.GPUSandbox.StableFluids
{
	/// <summary>
	/// シェーダのパス
	/// </summary>
	public enum SolverPass
	{
		Clear = 0,
		ClearBoundaries,
		Copy,
		CalcDivergence,
		CalcAndApplyVorticity,
		CalcAndApplyViscosity,
		CalcPressure,
		ApplyPressure,
		AdvectColor,
		AdvectVelocity,
		Mouse_Circle,
		Mouse_LineSeg,
		Draw_Circle,
		ApplyObstableMap,
		VeloicityColor,
		VorticityColor,
		PressureColor,
	}

	/// <summary>
	/// ソルバーの解像度
	/// </summary>
	public enum Resolution
	{
		x32 = 4,
		x64,
		x128,
		x256,
		x512,
		x1024,
	}

	/// <summary>
	/// 表示結果
	/// </summary>
	public enum View
	{
		All,
		Velocity,
		VelocityColor,
		Vorticity,
		Pressure,
		Texture
	}
}