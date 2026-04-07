// MimicFacilityPlayerState.h — Per-player replicated state: subject number, gear inventory, alive/converted status.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/PlayerState.h"
#include "MimicFacilityPlayerState.generated.h"

/**
 * AMimicFacilityPlayerState
 * Replicated per-player state. Tracks subject number, current gear loadout,
 * and whether the player is still alive or has been converted by a Mimic.
 */
UCLASS()
class MIMICFACILITY_API AMimicFacilityPlayerState : public APlayerState
{
	GENERATED_BODY()

public:
	AMimicFacilityPlayerState();

protected:
	/** Subject number (1–4) assigned to this player. */
	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Player")
	int32 SubjectNumber;

	/** Whether this player has been converted (eliminated) by a Mimic. */
	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Player")
	bool bIsConverted;

	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
};
