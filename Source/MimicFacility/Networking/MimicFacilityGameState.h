// MimicFacilityGameState.h — Replicated game state holding session-wide data: Mimic count, round info, voice data references.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameStateBase.h"
#include "MimicFacilityGameState.generated.h"

/**
 * AMimicFacilityGameState
 * Server-authoritative game state replicated to all clients.
 * Tracks active Mimic count, current round, and session-level data
 * used by the Director AI for state evaluation.
 */
UCLASS()
class MIMICFACILITY_API AMimicFacilityGameState : public AGameStateBase
{
	GENERATED_BODY()

public:
	AMimicFacilityGameState();

protected:
	/** Total number of active Mimics in the facility. */
	UPROPERTY(Replicated, BlueprintReadOnly, Category = "GameState")
	int32 ActiveMimicCount;

	/** Total number of Mimics that have been contained this session. */
	UPROPERTY(Replicated, BlueprintReadOnly, Category = "GameState")
	int32 ContainedMimicCount;

	/** Number of false-positive containment attempts (used a device on a real player). */
	UPROPERTY(Replicated, BlueprintReadOnly, Category = "GameState")
	int32 FalsePositiveCount;

	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
};
