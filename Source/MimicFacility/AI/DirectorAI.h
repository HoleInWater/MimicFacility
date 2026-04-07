// DirectorAI.h — The Director: facility AI with LLM integration, corruption tracking, personal weapon system.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "LLM/PromptBuilder.h"
#include "DirectorAI.generated.h"

class UOllamaClient;
class UPromptBuilder;
class UCorruptionTracker;
class UDirectorMemory;
class UPersonalWeaponSystem;

DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FOnDirectorStateChanged, EDirectorPhase, OldPhase, EDirectorPhase, NewPhase);
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

	// Phase control
	UFUNCTION(BlueprintCallable, Category = "Director")
	void SetPhase(EDirectorPhase NewPhase);

	UFUNCTION(BlueprintPure, Category = "Director")
	EDirectorPhase GetCurrentPhase() const { return CurrentPhase; }

	// Speech
	UFUNCTION(BlueprintCallable, Category = "Director")
	void Speak(const FString& DialogueLine);

	UFUNCTION(BlueprintCallable, Category = "Director")
	void RequestLLMDialogue(const FString& EventContext);

	UFUNCTION(BlueprintCallable, Category = "Director")
	void SpeakFallbackLine(const FString& EventContext);

	// Subsystems
	UFUNCTION(BlueprintPure, Category = "Director")
	UCorruptionTracker* GetCorruptionTracker() const { return CorruptionTracker; }

	UFUNCTION(BlueprintPure, Category = "Director")
	UDirectorMemory* GetMemory() const { return Memory; }

	UFUNCTION(BlueprintPure, Category = "Director")
	UPersonalWeaponSystem* GetWeaponSystem() const { return WeaponSystem; }

	UFUNCTION(BlueprintPure, Category = "Director")
	UOllamaClient* GetLLMClient() const { return LLMClient; }

	// Initialization
	UFUNCTION(BlueprintCallable, Category = "Director")
	void InitializeForSession(const TArray<FString>& PlayerIDs, const TArray<FString>& DisplayNames);

	// Delegates
	UPROPERTY(BlueprintAssignable)
	FOnDirectorStateChanged OnPhaseChanged;

	UPROPERTY(BlueprintAssignable)
	FOnDirectorSpeak OnDirectorSpeak;

	// Config
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Director")
	float DialogueInterval;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Director")
	FString LLMModelName;

protected:
	UPROPERTY()
	EDirectorPhase CurrentPhase;

	UPROPERTY()
	TObjectPtr<UOllamaClient> LLMClient;

	UPROPERTY()
	TObjectPtr<UPromptBuilder> PromptBuilder;

	UPROPERTY()
	TObjectPtr<UCorruptionTracker> CorruptionTracker;

	UPROPERTY()
	TObjectPtr<UDirectorMemory> Memory;

	UPROPERTY()
	TObjectPtr<UPersonalWeaponSystem> WeaponSystem;

private:
	void EvaluateGameState();
	void OnLLMResponse(const FString& Response);
	void OnLLMError(const FString& Error);

	FDirectorContext BuildCurrentContext() const;
	TArray<FString> GetFallbackLines(EDirectorPhase Phase) const;

	FTimerHandle StateEvaluationTimer;
	float TimeSinceLastDialogue;
	bool bHasSpokenFirstLine;
	bool bFirstPersonUsed;
};
