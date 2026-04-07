// MimicDialogueManager.h — Coordinates when and how mimics speak, managing dialogue queues and impersonation targeting.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "MimicDialogueManager.generated.h"

class AMimicBase;
class UPromptBuilder;
class UVoiceLearningSubsystem;

USTRUCT(BlueprintType)
struct FMimicDialogueEntry
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly)
	int32 MimicID = 0;

	UPROPERTY(BlueprintReadOnly)
	FString TargetPlayerID;

	UPROPERTY(BlueprintReadOnly)
	FString DialogueText;

	UPROPERTY(BlueprintReadOnly)
	float Priority = 0.0f;

	UPROPERTY(BlueprintReadOnly)
	float Timestamp = 0.0f;
};

UCLASS()
class MIMICFACILITY_API AMimicDialogueManager : public AActor
{
	GENERATED_BODY()

public:
	AMimicDialogueManager();

	virtual void BeginPlay() override;

	UFUNCTION(BlueprintCallable, Category = "Dialogue")
	void RegisterMimic(AMimicBase* Mimic);

	UFUNCTION(BlueprintCallable, Category = "Dialogue")
	void UnregisterMimic(AMimicBase* Mimic);

	UFUNCTION(BlueprintCallable, Category = "Dialogue")
	void RequestDialogue(const FMimicDialogueEntry& Entry);

	UFUNCTION(BlueprintCallable, Category = "Dialogue")
	FString SelectImpersonationTarget(AMimicBase* Mimic) const;

protected:
	void EvaluateDialogueOpportunities();
	void ProcessNextDialogue();
	bool IsPlayerLookingAtMimic(APlayerController* PC, AMimicBase* Mimic) const;

	UPROPERTY()
	TArray<TObjectPtr<AMimicBase>> ActiveMimics;

	TArray<FMimicDialogueEntry> DialogueQueue;

	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Dialogue")
	bool bDialogueInProgress;

	UPROPERTY(EditDefaultsOnly, Category = "Dialogue")
	int32 MaxSimultaneousSpeakers = 1;

	UPROPERTY(EditDefaultsOnly, Category = "Dialogue")
	float GlobalCooldown = 20.0f;

	UPROPERTY(EditDefaultsOnly, Category = "Dialogue")
	float PerMimicCooldown = 45.0f;

	UPROPERTY(EditDefaultsOnly, Category = "Dialogue")
	float EvaluationInterval = 5.0f;

	UPROPERTY(EditDefaultsOnly, Category = "Dialogue")
	float ProximityThreshold = 2000.0f;

	float LastGlobalDialogueTime;
	TMap<int32, float> PerMimicLastDialogueTime;
	FTimerHandle EvaluationTimerHandle;

	UPROPERTY()
	TObjectPtr<UPromptBuilder> PromptBuilder;

	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
};
