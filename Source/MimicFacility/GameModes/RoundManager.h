// RoundManager.h — Manages round transitions, Mimic spawning, and escalation pacing.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "RoundManager.generated.h"

UENUM(BlueprintType)
enum class ERoundPhase : uint8
{
	Round1_Exploration  UMETA(DisplayName = "Round 1 - Exploration"),
	Round2_Infiltration UMETA(DisplayName = "Round 2 - Infiltration"),
	Round3_Escalation   UMETA(DisplayName = "Round 3+ - Escalation"),
	GameOver            UMETA(DisplayName = "Game Over")
};

DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FOnRoundChanged, int32, NewRound, ERoundPhase, NewPhase);

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

	UFUNCTION(BlueprintCallable, Category = "Rounds")
	void AdvanceRound();

	UFUNCTION(BlueprintPure, Category = "Rounds")
	ERoundPhase GetCurrentPhase() const { return CurrentPhase; }

	UFUNCTION(BlueprintPure, Category = "Rounds")
	int32 GetRoundNumber() const { return RoundNumber; }

	UPROPERTY(BlueprintAssignable, Category = "Rounds")
	FOnRoundChanged OnRoundChanged;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Rounds")
	float Round1Duration;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Rounds")
	float RoundDuration;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Rounds")
	int32 MimicsPerRound;

protected:
	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Rounds")
	ERoundPhase CurrentPhase;

	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Rounds")
	int32 RoundNumber;

	UPROPERTY(BlueprintReadOnly, Category = "Rounds")
	float RoundTimer;

	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;

private:
	void SpawnMimicsForRound();
};
