// DirectorAI.h — The Director: omniscient facility AI that observes, misleads, and manipulates players.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "DirectorAI.generated.h"

/**
 * EDirectorState
 * Behavioral states for The Director AI.
 */
UENUM(BlueprintType)
enum class EDirectorState : uint8
{
	Observing       UMETA(DisplayName = "Observing"),
	Misleading      UMETA(DisplayName = "Misleading"),
	Escalating      UMETA(DisplayName = "Escalating"),
	Withdrawing     UMETA(DisplayName = "Withdrawing")
};

/**
 * ADirectorAI
 * Singleton actor that runs on the server. Monitors game state, generates dialogue
 * via Claude API, controls facility systems (doors, lights, spore vents), and
 * transitions between behavioral states to modulate player tension.
 */
UCLASS()
class MIMICFACILITY_API ADirectorAI : public AActor
{
	GENERATED_BODY()

public:
	ADirectorAI();

protected:
	virtual void BeginPlay() override;

public:
	virtual void Tick(float DeltaTime) override;

protected:
	/** Current behavioral state of The Director. */
	UPROPERTY(BlueprintReadOnly, Category = "Director")
	EDirectorState CurrentState;

	/** Timer handle for periodic game state evaluation. */
	FTimerHandle StateEvaluationTimer;

	/** Evaluate game state and potentially transition Director state. */
	UFUNCTION()
	void EvaluateGameState();
};
