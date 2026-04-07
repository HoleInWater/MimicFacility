// MimicFacility.Build.cs — Build configuration for the MimicFacility module.
// Copyright (c) 2026 HoleInWater. All rights reserved.

using UnrealBuildTool;

public class MimicFacility : ModuleRules
{
	public MimicFacility(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

		PublicDependencyModuleNames.AddRange(new string[]
		{
			"Core",
			"CoreUObject",
			"Engine",
			"InputCore",
			"AIModule",
			"GameplayTasks",
			"NavigationSystem",
			"UMG",
			"OnlineSubsystem",
			"OnlineSubsystemUtils"
		});

		PrivateDependencyModuleNames.AddRange(new string[]
		{
			"Slate",
			"SlateCore",
			"HTTP",
			"Json",
			"JsonUtilities"
		});
	}
}
