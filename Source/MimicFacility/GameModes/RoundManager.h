// RoundManager.h — Manages round transitions, Mimic spawning schedules, and escalation pacing.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "RoundManager.generated.h"

/**
 * ERoundPhase
 * The current phase of gameplay.
 */
UENUM(BlueprintType)
enum class ERoundPhase : uint8
{
	Round1_Exploration  UMETA(DisplayName = "Round 1 - Exploration"),
	Round2_Infiltration UMETA(DisplayName = "Round 2 - Infiltration"),
	Round3_Escalation   UMETA(DisplayName = "Round 3+ - Escalation"),
	GameOver            UMETA(DisplayName = "Game Over")
};

/**
 * ARoundManager
 * Server-authoritative actor that controls round transitions, triggers Mimic spawns
 * at the start of each round, and manages escalation timers and thresholds.
 */
UCLASS()
class MIMICFACILITY_API ARoundManager : public AActor
{
	GENERATED_BODY()

public:
	ARoundManager();

protected:
	virtual void BeginPlay() override;

public:
	virtual void Tick(float DeltaTime) override;

	/** Advance to the next round phase. */
	UFUNCTION(BlueprintCallable, Category = "Rounds")
	void AdvanceRound();

protected:
	/** Current round phase. */
	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Rounds")
	ERoundPhase CurrentPhase;

	/** Current round number (1, 2, 3, ...). */
	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Rounds")
	int32 RoundNumber;

	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
};
