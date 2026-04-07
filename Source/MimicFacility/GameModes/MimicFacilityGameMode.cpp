// MimicFacilityGameMode.cpp — Primary game mode implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicFacilityGameMode.h"
#include "Characters/MimicFacilityCharacter.h"
#include "UI/MimicFacilityHUD.h"

AMimicFacilityGameMode::AMimicFacilityGameMode()
{
	DefaultPawnClass = AMimicFacilityCharacter::StaticClass();
	HUDClass = AMimicFacilityHUD::StaticClass();
}

void AMimicFacilityGameMode::BeginPlay()
{
	Super::BeginPlay();
	// TODO: Spawn DirectorAI actor, initialize RoundManager.
}
