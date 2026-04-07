// MimicFacility.Target.cs — Build target for the game (non-editor).
// Copyright (c) 2026 HoleInWater. All rights reserved.

using UnrealBuildTool;
using System.Collections.Generic;

public class MimicFacilityTarget : TargetRules
{
	public MimicFacilityTarget(TargetInfo Target) : base(Target)
	{
		Type = TargetType.Game;
		DefaultBuildSettings = BuildSettingsVersion.V4;
		IncludeOrderVersion = EngineIncludeOrderVersion.Unreal5_4;
		ExtraModuleNames.Add("MimicFacility");
	}
}
