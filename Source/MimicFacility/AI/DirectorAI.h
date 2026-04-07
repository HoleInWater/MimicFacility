// DirectorAI.h — The Director: omniscient facility AI that observes, misleads, and manipulates players.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "DirectorAI.generated.h"

UENUM(BlueprintType)
enum class EDirectorState : uint8
{
	Observing       UMETA(DisplayName = "Observing"),
	Misleading      UMETA(DisplayName = "Misleading"),
	Escalating      UMETA(DisplayName = "Escalating"),
	Withdrawing     UMETA(DisplayName = "Withdrawing")
};

DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FOnDirectorStateChanged, EDirectorState, OldState, EDirectorState, NewState);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnDirectorSpeak, const FString&, DialogueLine);

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

	UFUNCTION(BlueprintCallable, Category = "Director")
	void SetDirectorState(EDirectorState NewState);

	UFUNCTION(BlueprintCallable, Category = "Director")
	EDirectorState GetDirectorState() const { return CurrentState; }

	UFUNCTION(BlueprintCallable, Category = "Director")
	void Speak(const FString& DialogueLine);

	UPROPERTY(BlueprintAssignable, Category = "Director")
	FOnDirectorStateChanged OnDirectorStateChanged;

	UPROPERTY(BlueprintAssignable, Category = "Director")
	FOnDirectorSpeak OnDirectorSpeak;

protected:
	UPROPERTY(BlueprintReadOnly, Category = "Director")
	EDirectorState CurrentState;

	FTimerHandle StateEvaluationTimer;

	UFUNCTION()
	void EvaluateGameState();

	// Fallback dialogue pools per state
	TArray<FString> GetFallbackDialogue(EDirectorState State) const;

	float TimeSinceLastDialogue;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Director")
	float DialogueInterval;
};
