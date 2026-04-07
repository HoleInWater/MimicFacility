// VoiceLearningSubsystem.h — Captures, processes, and stores player voice data for Mimic impersonation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "VoiceLearningSubsystem.generated.h"

USTRUCT(BlueprintType)
struct FVoicePhrase
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly)
	FString Text;

	UPROPERTY(BlueprintReadOnly)
	float SessionTimestamp;

	UPROPERTY(BlueprintReadOnly)
	TArray<FString> WitnessPlayerIDs;
};

UCLASS()
class MIMICFACILITY_API UVoiceLearningSubsystem : public UGameInstanceSubsystem
{
	GENERATED_BODY()

public:
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;
	virtual void Deinitialize() override;

	UFUNCTION(BlueprintCallable, Category = "Voice")
	TArray<FVoicePhrase> GetPhrasesForPlayer(const FString& PlayerID) const;

	UFUNCTION(BlueprintCallable, Category = "Voice")
	TArray<FString> GetTriggerWords(const FString& PlayerID) const;

	// Test/debug methods for manual data injection
	UFUNCTION(BlueprintCallable, Category = "Voice|Debug")
	void DebugAddPhrase(const FString& PlayerID, const FString& PhraseText, const TArray<FString>& Witnesses);

	UFUNCTION(BlueprintCallable, Category = "Voice|Debug")
	void DebugSetTriggerWords(const FString& PlayerID, const TArray<FString>& Words);

	UFUNCTION(BlueprintCallable, Category = "Voice")
	void SelectTriggerWordsForAllPlayers();

	UFUNCTION(BlueprintCallable, Category = "Voice")
	bool CheckTriggerWord(const FString& PlayerID, const FString& SpokenWord) const;

	UFUNCTION(BlueprintPure, Category = "Voice")
	int32 GetPhraseCount(const FString& PlayerID) const;

	UFUNCTION(BlueprintCallable, Category = "Voice")
	void ClearAllData();

protected:
	TMap<FString, TArray<FVoicePhrase>> PhraseDatabase;
	TMap<FString, TArray<FString>> TriggerWordAssignments;
};
