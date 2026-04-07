// MimicFacilityHUD.h — Main HUD class. Manages widget display for gear, round info, and Director messages.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/HUD.h"
#include "MimicFacilityHUD.generated.h"

/**
 * AMimicFacilityHUD
 * The player's heads-up display. Creates and manages UMG widgets for
 * gear inventory, round indicators, proximity alerts, and Director message feed.
 */
UCLASS()
class MIMICFACILITY_API AMimicFacilityHUD : public AHUD
{
	GENERATED_BODY()

public:
	AMimicFacilityHUD();

protected:
	virtual void BeginPlay() override;

public:
	virtual void DrawHUD() override;
};
