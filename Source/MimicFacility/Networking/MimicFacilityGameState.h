// MimicFacilityGameState.h — Replicated game state: Mimic counts, round info, session-wide tracking.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameStateBase.h"
#include "MimicFacilityGameState.generated.h"

UCLASS()
class MIMICFACILITY_API AMimicFacilityGameState : public AGameStateBase
{
	GENERATED_BODY()

public:
	AMimicFacilityGameState();

	// Getters
	UFUNCTION(BlueprintPure, Category = "GameState")
	int32 GetActiveMimicCount() const { return ActiveMimicCount; }

	UFUNCTION(BlueprintPure, Category = "GameState")
	int32 GetContainedMimicCount() const { return ContainedMimicCount; }

	UFUNCTION(BlueprintPure, Category = "GameState")
	int32 GetFalsePositiveCount() const { return FalsePositiveCount; }

	UFUNCTION(BlueprintPure, Category = "GameState")
	int32 GetCurrentRound() const { return CurrentRound; }

	// Modifiers (server only)
	UFUNCTION(BlueprintCallable, Category = "GameState")
	void AddActiveMimic();

	UFUNCTION(BlueprintCallable, Category = "GameState")
	void RemoveActiveMimic();

	UFUNCTION(BlueprintCallable, Category = "GameState")
	void IncrementContained();

	UFUNCTION(BlueprintCallable, Category = "GameState")
	void IncrementFalsePositive();

	UFUNCTION(BlueprintCallable, Category = "GameState")
	void SetCurrentRound(int32 Round);

protected:
	UPROPERTY(Replicated, BlueprintReadOnly, Category = "GameState")
	int32 ActiveMimicCount;

	UPROPERTY(Replicated, BlueprintReadOnly, Category = "GameState")
	int32 ContainedMimicCount;

	UPROPERTY(Replicated, BlueprintReadOnly, Category = "GameState")
	int32 FalsePositiveCount;

	UPROPERTY(Replicated, BlueprintReadOnly, Category = "GameState")
	int32 CurrentRound;

	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
};
