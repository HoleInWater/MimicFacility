// MimicFacilityEditor.Target.cs — Build target for the Unreal Editor.
// Copyright (c) 2026 HoleInWater. All rights reserved.

using UnrealBuildTool;
using System.Collections.Generic;

public class MimicFacilityEditorTarget : TargetRules
{
	public MimicFacilityEditorTarget(TargetInfo Target) : base(Target)
	{
		Type = TargetType.Editor;
		DefaultBuildSettings = BuildSettingsVersion.V4;
		IncludeOrderVersion = EngineIncludeOrderVersion.Unreal5_4;
		ExtraModuleNames.Add("MimicFacility");
	}
}
