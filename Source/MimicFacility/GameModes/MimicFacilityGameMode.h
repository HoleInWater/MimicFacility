// MimicFacilityGameMode.h — Primary game mode. Sets default pawn, HUD, game state, and player state classes.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "MimicFacilityGameMode.generated.h"

/**
 * AMimicFacilityGameMode
 * The primary game mode for MimicFacility. Server-authoritative.
 * Configures default classes for pawn, HUD, game state, and player state.
 * Owns the round management lifecycle.
 */
UCLASS()
class MIMICFACILITY_API AMimicFacilityGameMode : public AGameModeBase
{
	GENERATED_BODY()

public:
	AMimicFacilityGameMode();

protected:
	virtual void BeginPlay() override;
};
